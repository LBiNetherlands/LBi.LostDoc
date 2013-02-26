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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public static class ContractNames
    {
        public const string AdminController = "AdministrationController";
    }

    public interface IControllerMetadata
    {
        string Name { get; }
        string Group { get; }
        double Order { get; }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AdminActionAttribute : Attribute
    {
        public AdminActionAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; protected set; }
        
        public bool IsDefault { get; set; }
        public double Order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AdminControllerAttribute : ExportAttribute, IControllerMetadata
    {
        public AdminControllerAttribute(string name) : base(ContractNames.AdminController, typeof(IController))
        {
            this.Name = name;
        }

        public string Name { get; protected set; }

        public string Group { get; set; }
        public double Order { get; set; }
    }
}
