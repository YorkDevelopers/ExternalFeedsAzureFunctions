#load "childTag.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// Represents an HTML document
/// </summary>
public class Document
{
    private string html;

    public Document(string html)
    {
        this.html = html;
    }

    internal ChildTag GetNextTag(string toFind)
    {
        var startOfTag = this.html.IndexOf(toFind);
        if (startOfTag > 0)
        {
            return new ChildTag(startOfTag, startOfTag + toFind.Length, this.html.Substring(startOfTag, toFind.Length));
        }

        return null;

    }

    internal ChildTag GetNextTagOfType(string tagType, ChildTag afterTag)
    {
        var startOfTag = this.html.IndexOf("<" + tagType, afterTag.EndOfTag);
        if (startOfTag > 0)
        {
            var endOfStartTag = this.html.IndexOf(">", startOfTag + 1);
            if (endOfStartTag > 0)
            {
                var endOfTag = this.html.IndexOf("</" + tagType + ">", endOfStartTag + 1);
                if (endOfTag == -1)
                    endOfTag = this.html.IndexOf("/>", endOfStartTag + 1);
                if (endOfTag > 0)
                {
                    return new ChildTag(startOfTag, endOfTag, this.html.Substring(endOfStartTag + 1, endOfTag - endOfStartTag - 1));
                }
            }
        }

        return null;
    }


    internal string GetAttribute(ChildTag urlTag, string attributeName)
    {
        var startOfAttributes = this.html.IndexOf(attributeName + @"=""", urlTag.StartOfTag);
        if (startOfAttributes > 0 && startOfAttributes < urlTag.EndOfTag)
        {
            startOfAttributes += (attributeName + @"=""").Length;
            var endOfAttribute = this.html.IndexOf(@"""", startOfAttributes);
            if (endOfAttribute > 0)
            {
                return this.html.Substring(startOfAttributes, endOfAttribute - startOfAttributes);
            }
        }

        return "";
    }

    internal ChildTag GetNextTag(string toFind, ChildTag afterTag)
    {
        var startOfTag = this.html.IndexOf(toFind, afterTag.EndOfTag + 1);
        if (startOfTag > 0)
        {
            return new ChildTag(startOfTag, startOfTag + toFind.Length, this.html.Substring(startOfTag, toFind.Length));
        }

        return null;
    }
}
