namespace Company.Project.Library
{
    /// <summary>
    /// This is a simple generic class that inherits <see cref="SecondLevelSimpleGeneric{T4,T5}"/>
    /// </summary>
    /// <typeparam name="T6">
    /// </typeparam>
    public class ThirdLevelSimpleGeneric<T6> : SecondLevelSimpleGeneric<int, T6>
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
        public override void SimpleTest(string one, int two, T6 three)
        {
            base.SimpleTest(one, two, three);
        }
    }
}