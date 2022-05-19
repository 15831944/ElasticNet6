#nullable disable

namespace Elastic.Docs;

public class NameValueDoc<TName, TValue>
{
    public TName Name { get; set; }
    public TValue Value { get; set; }
}
