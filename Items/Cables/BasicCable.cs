namespace PotentiaTransport.Items.Cables
{
	public class BasicCable : BaseCable
	{
		public override int MaxIO => 1000;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Basic Wire");
		}
	}
}