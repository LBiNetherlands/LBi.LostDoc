/*
 * Copyright 2013 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace LBi.LostDoc.Composition
{
    // TODO this turned into a crazy amount of code just to make the API for using it nicer, is it really worth it?
    public class MetadataContractBuilder<T, TMetadata>
    {
        // TODO this probably not what I wanted
        private static long _counter = 0;

        private readonly List<Expression<Func<TMetadata, TMetadata, bool>>> _constraints;
        private readonly List<KeyValuePair<string, Type>> _metadata;
        private readonly ImportCardinality _cardinality;
        private readonly CreationPolicy _creationPolicy;
        private Func<TMetadata, ExportDefinition, bool> _constraint;
        private Type _metadataType;
        private Func<TMetadata> _metadataCtor;
        private Action<TMetadata, object>[] _propSetters;
        private PropertyInfo[] _interfaceProperties;
        private string _contractName;


        public MetadataContractBuilder(string contractName, ImportCardinality importCardinality, CreationPolicy creationPolicy)
        {
            this._contractName = contractName ?? AttributedModelServices.GetContractName(typeof(T));
            this._cardinality = importCardinality;
            this._creationPolicy = creationPolicy;
            this._constraints = new List<Expression<Func<TMetadata, TMetadata, bool>>>();
            this._metadata = new List<KeyValuePair<string, Type>>();
        }

        public MetadataContractBuilder(ImportCardinality importCardinality, CreationPolicy creationPolicy)
            : this(null, importCardinality, creationPolicy)
        {

        }

        public void Prepare()
        {
            if (this._constraint == null)
            {
                this._constraint = this.CompileConstraint(this._constraints);
                this.CreateMetadataImpl(typeof(TMetadata));
            }
        }


        private Func<TMetadata, ExportDefinition, bool> CompileConstraint(IEnumerable<Expression<Func<TMetadata, TMetadata, bool>>> constraints)
        {
            ParameterExpression exportDefParam = Expression.Parameter(typeof(ExportDefinition), "exportDefinition");
            ParameterExpression contractParam = Expression.Parameter(typeof(TMetadata), "@contract");

            ParameterExpression metadataParam = Expression.Variable(typeof(TMetadata), "@metadata");

            MethodInfo convertMethod = typeof(AttributedModelServices).GetMethod("GetMetadataView",
                                                                                 BindingFlags.Static | BindingFlags.Public);

            // make generic
            convertMethod = convertMethod.MakeGenericMethod(typeof(TMetadata));

            Expression metadataExpr = Expression.Property(exportDefParam, typeof(ExportDefinition), "Metadata");
            Expression convertExpr = Expression.Assign(metadataParam,
                                                       Expression.Call(null, convertMethod, metadataExpr));
            Expression bodyExpr = null;
            foreach (Expression<Func<TMetadata, TMetadata, bool>> constraint in constraints)
            {
                Expression newConstraint = new ParameterRewriter(constraint.Parameters[1], metadataParam).Visit(constraint.Body);
                newConstraint = new ParameterRewriter(constraint.Parameters[0], contractParam).Visit(newConstraint);

                if (bodyExpr == null)
                    bodyExpr = newConstraint;
                else
                    bodyExpr = Expression.AndAlso(bodyExpr, newConstraint);
            }

            if (bodyExpr == null)
                bodyExpr = Expression.Constant(true, typeof(bool));

            Expression<Func<TMetadata, ExportDefinition, bool>> lambdaExpr =
                Expression.Lambda<Func<TMetadata, ExportDefinition, bool>>(
                    Expression.Block(new[] { metadataParam }, convertExpr, bodyExpr), contractParam, exportDefParam);

            return lambdaExpr.Compile();
        }

        private void CreateMetadataImpl(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Not an interface.", "interfaceType");

            AssemblyName assemblyName =
                new AssemblyName(string.Format("LBi.LostDoc.Composition.MetadataContractBuilder`2____<{0}>{1}",
                                               Interlocked.Increment(ref _counter),
                                               interfaceType.Name));

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
                                                                                            AssemblyBuilderAccess.RunAndCollect);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("RunTimeCompiled");

            TypeBuilder typeBuilder = moduleBuilder.DefineType(interfaceType.Name.Substring(1) + "Proxy",
                                                               TypeAttributes.Public |
                                                               TypeAttributes.Sealed |
                                                               TypeAttributes.Class,
                                                               typeof(object),
                                                               new[] { interfaceType });

            List<PropertyInfo> interfaceProperties = new List<PropertyInfo>();
            interfaceProperties.AddRange(interfaceType.GetProperties());
            foreach (var inheritedInterfaceType in interfaceType.GetInterfaces())
                interfaceProperties.AddRange(inheritedInterfaceType.GetProperties());

            this._interfaceProperties = interfaceProperties.ToArray();

            FieldBuilder[] fields = new FieldBuilder[this._interfaceProperties.Length];
            PropertyBuilder[] realProperties = new PropertyBuilder[this._interfaceProperties.Length];


            for (int i = 0; i < this._interfaceProperties.Length; i++)
            {
                // 1. create one field per property
                fields[i] = typeBuilder.DefineField("_" + this._interfaceProperties[i].Name,
                                                    this._interfaceProperties[i].PropertyType,
                                                    FieldAttributes.Public);


                // 2. create property getter
                realProperties[i] = typeBuilder.DefineProperty(this._interfaceProperties[i].Name,
                                                                PropertyAttributes.HasDefault,
                                                                CallingConventions.ExplicitThis,
                                                                this._interfaceProperties[i].PropertyType,
                                                                Type.EmptyTypes);


                MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + this._interfaceProperties[i].Name,
                                                                   MethodAttributes.Public
                                                                   | MethodAttributes.Final
                                                                   | MethodAttributes.Virtual
                                                                   | MethodAttributes.HideBySig
                                                                   | MethodAttributes.VtableLayoutMask
                                                                   | MethodAttributes.SpecialName,
                                                                   CallingConventions.HasThis,
                                                                   this._interfaceProperties[i].PropertyType,
                                                                   Type.EmptyTypes);


                ILGenerator ilGen = getMethod.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, fields[i]);
                ilGen.Emit(OpCodes.Ret);

                realProperties[i].SetGetMethod(getMethod);
            }

            // 3. create a CTOR
            ConstructorInfo defaultCtor = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            this._metadataType = typeBuilder.CreateType();

            this._metadataCtor = Expression.Lambda<Func<TMetadata>>(Expression.New(this._metadataType)).Compile();
            this._propSetters = new Action<TMetadata, object>[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                ParameterExpression instanceParam = Expression.Parameter(interfaceType, "this");
                ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
                this._propSetters[i] =
                    Expression.Lambda<Action<TMetadata, object>>(
                        Expression.Assign(Expression.Field(Expression.Convert(instanceParam, this._metadataType),
                                                           fields[i].Name),
                                          Expression.Convert(valueParam, fields[i].FieldType)), instanceParam,
                        valueParam).Compile();
            }
        }

        public void Add(Expression<Func<TMetadata, TMetadata, bool>> constraint)
        {
            // hold on to for usage later
            this._constraints.Add(constraint);
            // find all Property references on TMetadata
            PropertyRefFinder refFinder = new PropertyRefFinder(constraint.Parameters[0]);
            refFinder.Visit(constraint);

            // add to metadata contract list
            foreach (PropertyInfo propertyInfo in refFinder)
                this._metadata.Add(new KeyValuePair<string, Type>(propertyInfo.Name, propertyInfo.PropertyType));
        }

        private class MetadataContract : ContractBasedImportDefinition
        {
            private readonly Func<TMetadata, ExportDefinition, bool> _constraint;
            private readonly TMetadata _contract;

            public MetadataContract(string contractName,
                                    IEnumerable<KeyValuePair<string, Type>> metadata,
                                    Func<TMetadata, ExportDefinition, bool> constraint,
                                    ImportCardinality importCardinality,
                                    CreationPolicy creationPolicy,
                TMetadata contract)
                : base(contractName,
                       AttributedModelServices.GetContractName(typeof(T)),
                       metadata,
                       importCardinality,
                       false,
                       false,
                       creationPolicy)
            {
                this._constraint = constraint;
                this._contract = contract;
            }

            public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
            {
                bool ret = base.IsConstraintSatisfiedBy(exportDefinition);

                if (ret)
                    ret = this._constraint(_contract, exportDefinition);

                return ret;
            }
        }

        private class PropertyRefFinder : ExpressionVisitor, IEnumerable<PropertyInfo>
        {
            private readonly ParameterExpression _metadataParam;
            private readonly List<PropertyInfo> _refs;

            public PropertyRefFinder(ParameterExpression metadataParam)
            {
                this._metadataParam = metadataParam;
                this._refs = new List<PropertyInfo>();
            }
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member is PropertyInfo && node.Expression == this._metadataParam)
                    this._refs.Add((PropertyInfo)node.Member);
                return base.VisitMember(node);
            }

            public IEnumerator<PropertyInfo> GetEnumerator()
            {
                return this._refs.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class ParameterRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly Expression _newExpr;

            public ParameterRewriter(ParameterExpression oldParam, Expression newExpr)
            {
                this._oldParam = oldParam;
                this._newExpr = newExpr;
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == this._oldParam)
                    return this._newExpr;

                return base.VisitParameter(node);
            }
        }

        public class MetadataValueBuilder
        {
            private readonly MetadataContractBuilder<T, TMetadata> _owner;
            private TMetadata _instance;

            public MetadataValueBuilder(MetadataContractBuilder<T, TMetadata> owner)
            {
                this._owner = owner;
                this._instance = this._owner._metadataCtor();
            }

            public MetadataValueBuilder WithValue<TReturn>(Expression<Func<TMetadata, TReturn>> propertyAccessor, TReturn value)
            {
                MemberExpression mexpr = propertyAccessor.Body as MemberExpression;
                if (mexpr == null)
                    throw new ArgumentException("Only property access is allowed.", "propertyAccessor");

                PropertyInfo propInfo = mexpr.Member as PropertyInfo;
                if (propInfo == null)
                    throw new ArgumentException("Only property access is allowed.", "propertyAccessor");


                this.SetValue(propInfo, value);


                return this;
            }

            private void SetValue<TValue>(PropertyInfo interfacePropInfo, TValue value)
            {
                // TODO figure out if this can be made faster, sorted list & binary search, dictionary, based on interfacePropInfo.MetadataToken?

                var ix = Array.IndexOf(this._owner._interfaceProperties, interfacePropInfo);
                this._owner._propSetters[ix](this._instance, value);
            }

            public static implicit operator ImportDefinition(MetadataValueBuilder valueBuilder)
            {
                return valueBuilder.GetImportDefinition();
            }

            public ImportDefinition GetImportDefinition()
            {
                return new MetadataContract(this._owner._contractName,
                                            this._owner._metadata,
                                            this._owner._constraint,
                                            this._owner._cardinality,
                                            this._owner._creationPolicy,
                                            this._instance);
            }
        }

        public MetadataValueBuilder WithValue<TReturn>(Expression<Func<TMetadata, TReturn>> propertyAccessor, TReturn value)
        {
            this.Prepare();
            return new MetadataValueBuilder(this).WithValue(propertyAccessor, value);
        }
    }
}