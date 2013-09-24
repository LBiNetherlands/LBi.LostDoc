using Xunit;

namespace LBi.Cci.Test
{
    public class HostTestBase : IUseFixture<HostFixture>
    {
        public void SetFixture(HostFixture data)
        {
            this.Fixture = data;
        }

        protected HostFixture Fixture { get; set; }
    }
}