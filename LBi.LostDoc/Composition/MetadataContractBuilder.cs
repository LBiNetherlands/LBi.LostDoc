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

namespace LBi.LostDoc.Composition
{
    public class MetadataContractBuilder<T, TMetadata>
    {
        private readonly List<Expression<Func<TMetadata, bool>>> _constraints;
        private readonly List<KeyValuePair<string, Type>> _metadata;
        private readonly ImportCardinality _cardinality;
        private readonly CreationPolicy _creationPolicy;

        public MetadataContractBuilder(ImportCardinality importCardinality, CreationPolicy creationPolicy)
        {
            this._cardinality = importCardinality;
            this._creationPolicy = creationPolicy;
            this._constraints = new List<Expression<Func<TMetadata, bool>>>();
            this._metadata = new List<KeyValuePair<string, Type>>();
        }

        public ImportDefinition GetImportDefinition()
        {
            return new MetadataContract(this._metadata,
                                        this.CompileConstraint(this._constraints),
                                        this._cardinality,
                                        this._creationPolicy);
        }

        private Func<ExportDefinition, bool> CompileConstraint(IEnumerable<Expression<Func<TMetadata, bool>>> constraints)
        {
            ParameterExpression exportDefParam = Expression.Parameter(typeof(ExportDefinition), "exportDefinition");

            ParameterExpression metadataParam = Expression.Variable(typeof(TMetadata), "metadata");


            MethodInfo convertMethod = typeof(AttributedModelServices).GetMethod("GetMetadataView",
                                                                                 BindingFlags.Static | BindingFlags.Public);

            // make generic
            convertMethod = convertMethod.MakeGenericMethod(typeof(TMetadata));

            Expression metadataExpr = Expression.Property(exportDefParam, typeof(ExportDefinition), "Metadata");
            Expression convertExpr = Expression.Assign(metadataParam,
                                                       Expression.Call(null, convertMethod, metadataExpr));
            Expression bodyExpr = null;
            foreach (Expression<Func<TMetadata, bool>> constraint in constraints)
            {
                Expression newConstraint = new ParameterRewriter(constraint.Parameters[0], metadataParam).Visit(constraint.Body);
                if (bodyExpr == null)
                    bodyExpr = newConstraint;
                else
                    bodyExpr = Expression.AndAlso(bodyExpr, newConstraint);
            }

            if (bodyExpr == null)
                bodyExpr = Expression.Constant(true, typeof(bool));

            Expression<Func<ExportDefinition, bool>> lambdaExpr =
                Expression.Lambda<Func<ExportDefinition, bool>>(
                    Expression.Block(new[] {metadataParam}, convertExpr, bodyExpr), exportDefParam);

            return lambdaExpr.Compile();
        }

        public void Add(Expression<Func<TMetadata, bool>> constraint)
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
            private readonly Func<ExportDefinition, bool> _constraint;

            public MetadataContract(IEnumerable<KeyValuePair<string, Type>> metadata,
                                    Func<ExportDefinition, bool> constraint,
                                    ImportCardinality importCardinality,
                                    CreationPolicy creationPolicy)
                : base(AttributedModelServices.GetContractName(typeof(T)),
                       null,
                       metadata,
                       importCardinality,
                       false,
                       false,
                       creationPolicy)
            {
                this._constraint = constraint;
            }


            public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
            {
                bool ret = base.IsConstraintSatisfiedBy(exportDefinition);

                if (ret)
                    ret = this._constraint(exportDefinition);

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
    }
}