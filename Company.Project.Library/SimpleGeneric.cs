namespace Company.Project.Library
{
    /// <summary>
    /// This is a simple generic class
    /// </summary>
    /// <typeparam name="T1">
    /// First param 
    /// </typeparam>
    /// <typeparam name="T2">
    /// Second param 
    /// </typeparam>
    /// <typeparam name="T3">
    /// Third param 
    /// </typeparam>
    public class SimpleGeneric<T1, T2, T3>
    {
        /// <summary>
        /// Virtual Test method
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
        public virtual void SimpleTest(T1 one, T2 two, T3 three)
        {
        }
    }
}