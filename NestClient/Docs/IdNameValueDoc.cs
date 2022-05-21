namespace Elastic.Docs;

public class IdNameValueDoc<TName, TValue>
{
    public string? Id { get; set; }
#nullable disable
    public TName Name { get; set; }
    public TValue Value { get; set; }
}
