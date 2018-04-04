using System;


/// <summary>
/// Represents an HTML child tag.  This is a tag which does not contain other tags.  For example <a href=...>content</a>
/// </summary>
public class ChildTag
{
    /// <summary>
    /// Start position in the document
    /// </summary>
    public int StartOfTag { get; private set; }

    /// <summary>
    /// End position in the document
    /// </summary>
    public int EndOfTag { get; private set; }

    /// <summary>
    /// Contents of the tag
    /// </summary>
    public string Contents { get; private set; }

    public ChildTag(int startOfTag, int endOfTag, string contents)
    {
        this.StartOfTag = startOfTag;
        this.EndOfTag = endOfTag;
        this.Contents = contents;
    }


}

