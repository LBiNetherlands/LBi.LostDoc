namespace LBi.LostDoc.Cci
{
    public class CciFilterContext : ICciFilterContext
    {
        public CciFilterContext(FilterState state)
        {
            this.State = state;
        }

        public FilterState State { get; protected set; }
    }
}