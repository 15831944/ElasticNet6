namespace Elastic;

public class ListItemParameter<TValue>
{
    public string Name { get; set; } = "Value";
    public TValue? Value { get; set; }
}