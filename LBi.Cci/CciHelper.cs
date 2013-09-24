using System;
using Microsoft.Cci;

namespace LBi.Cci
{
    public static class CciHelper
    {
        
        #region Assembly Extensions
        public static string GetFullName(this IAssembly assembly)
        {
            return UnitHelper.StrongName(assembly);
        }

        #endregion

        #region Type Extensions

        public static IAssembly GetAssembly(this ITypeReference typeRef)
        {
            return typeRef.ResolvedType.GetAssembly();
        }

        public static IAssembly GetAssembly(this ITypeDefinition typeDef)
        {
            INamespaceTypeDefinition nsTypeDef = typeDef as INamespaceTypeDefinition;
            if (nsTypeDef != null)
                return nsTypeDef.GetAssembly();
            
            throw new InvalidOperationException("cannot find assembly");
        }

        public static IAssembly GetAssembly(this INamespaceTypeDefinition typeDef)
        {
            return (IAssembly)typeDef.Container.RootOwner;
        }
        #endregion
    }
}
