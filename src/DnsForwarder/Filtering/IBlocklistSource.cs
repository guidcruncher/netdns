namespace DnsForwarder.Filtering;

public interface IBlocklistSource
{
    Task<IEnumerable<ParsedRule>> LoadAsync();
}
