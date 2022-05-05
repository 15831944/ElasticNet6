namespace Elastic.Docs;

#nullable disable

/// <summary>
/// Документ, аналогичный строке<br/>
/// <code>
/// code|text1|||textN|||||
/// </code><br/>
/// Каждая строка должна содержать code и хотя бы одну текстовую ячейку.
/// </summary>
public class CodeTextsDoc
{
    /// <summary>
    /// Идентификатор сущности.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Страна, город, номер телефона и т.п.<br/>
    /// Должен содержать хотя бы один элемент.
    /// </summary>
    public List<ListItemParameter<string>> TextParameters { get; set; }
}