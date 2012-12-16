namespace Company.Project.Library
{
    /// <summary>
    /// This is a simple generic class that inherits <see cref="SimpleGeneric{T1,T2,T3}"/>
    /// </summary>
    /// <typeparam name="T4">
    /// Param two 
    /// </typeparam>
    /// <typeparam name="T5">
    /// Param three 
    /// </typeparam>
    public class SecondLevelSimpleGeneric<T4, T5> : SimpleGeneric<string, T4, T5>
    {
        /// <summary>
        /// Override virtual test method
        /// </summary>
        /// <param name="one">
        /// Param one 
        /// </param>
        /// <param name="two">
        /// Param two 
        /// </param>
        /// <param name="three">
        /// Param three 
        /// </param>
        public override void SimpleTest(string one, T4 two, T5 three)
        {
        }
    }
}