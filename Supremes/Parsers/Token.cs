using Supremes.Helper;
using Supremes.Nodes;
using System.Text;

namespace Supremes.Parsers
{
    /// <summary>
    /// Parse tokens for the Tokeniser.
    /// </summary>
    internal abstract class Token
    {
        internal TokenType type;

        internal const int Unset = -1;

        internal Token()
        {
        }

        internal string Type => this.GetType().Name;

        internal virtual Token Reset()
        {
            StartPos = Unset;
            EndPos = Unset;
            return this;
        }

        internal int StartPos { get; set; }

        internal int EndPos { get; set; }

        internal static void Reset(StringBuilder sb)
        {
            sb?.Clear();
        }

        internal class Doctype : Token
        {
            internal readonly StringBuilder name = new StringBuilder();

            internal string pubSysKey = null;

            internal readonly StringBuilder publicIdentifier = new StringBuilder();

            internal readonly StringBuilder systemIdentifier = new StringBuilder();

            internal bool forceQuirks = false;

            public Doctype()
            {
                type = TokenType.Doctype;
            }

            internal override Token Reset()
            {
                base.Reset();
                Reset(name);
                pubSysKey = null;
                Reset(publicIdentifier);
                Reset(systemIdentifier);
                forceQuirks = false;
                return this;
            }

            internal string GetName()
            {
                return name.ToString();
            }
            
            internal string GetPubSysKey()
            {
                return pubSysKey;
            }

            internal string GetPublicIdentifier()
            {
                return publicIdentifier.ToString();
            }

            public string GetSystemIdentifier()
            {
                return systemIdentifier.ToString();
            }

            public bool IsForceQuirks()
            {
                return forceQuirks;
            }

            public override string ToString()
            {
                return "<!doctype " + GetName() + ">";
            }
        }

        internal abstract class Tag : Token
        {
            internal string tagName;
            internal string normalName;

            private readonly StringBuilder attrName = new StringBuilder();
            private string attrNameS;
            private bool hasAttrName = false;

            private readonly StringBuilder attrValue = new StringBuilder();
            private string attrValueS;
            private bool hasAttrValue = false;
            private bool hasEmptyAttrValue = false;

            public bool selfClosing = false;
            public Attributes attributes;

            internal override Token Reset()
            {
                base.Reset();
                tagName = null;
                normalName = null;
                Reset(attrName);
                attrNameS = null;
                hasAttrName = false;
                Reset(attrValue);
                attrValueS = null;
                hasEmptyAttrValue = false;
                hasAttrValue = false;
                selfClosing = false;
                attributes = null;
                return this;
            }
            
            private const int MaxAttributes = 512;
            
            // attribute names are generally caught in one hop, not accumulated
            // but values are accumulated, from e.g. & in hrefs
            // start tags get attributes on construction. End tags get attributes on first new attribute (but only for parser convenience, not used).
            internal void NewAttribute()
            {
                if (attributes == null)
                    attributes = new Attributes();

                if (hasAttrName && attributes.Count < MaxAttributes)
                {
                    string name = attrName.Length > 0 ? attrName.ToString() : attrNameS;
                    name = name.Trim();
                    if (name.Length > 0)
                    {
                        string value;
                        if (hasAttrValue)
                            value = attrValue.Length > 0 ? attrValue.ToString() : attrValueS;
                        else if (hasEmptyAttrValue)
                            value = "";
                        else
                            value = null;
                        attributes.Add(name, value);
                    }
                }
                Reset(attrName);
                attrNameS = null;
                hasAttrName = false;

                Reset(attrValue);
                attrValueS = null;
                hasAttrValue = false;
                hasEmptyAttrValue = false;
            }
            
            public bool HasAttributes()
            {
                return attributes != null;
            }
            
            public bool HasAttribute(string key)
            {
                return attributes != null && attributes.ContainsKey(key);
            }

            internal void FinaliseTag()
            {
                if (hasAttrName)
                {
                    NewAttribute();
                }
            }

            internal string Name()
            {
                Validate.IsFalse(string.IsNullOrEmpty(tagName));
                return tagName;
            }

            internal string NormalName()
            {
                return normalName!;
            }
            
            internal string ToStringName() {
                return tagName ?? "[unset]";
            }
            
            internal Token.Tag Name(string name)
            {
                tagName = name;
                normalName = ParseSettings.NormalName(tagName);
                return this;
            }

            internal bool IsSelfClosing()
            {
                return selfClosing;
            }

            internal Attributes GetAttributes()
            {
                return attributes;
            }

            // these appenders are rarely hit in not null state-- caused by null chars.
            internal void AppendTagName(string append)
            {
                // might have null chars - need to replace with null replacement character
                append = append.Replace(TokeniserState.nullChar, Tokeniser.replacementChar);
                tagName = tagName == null ? append : string.Concat(tagName, append);
                normalName = ParseSettings.NormalName(tagName);
            }

            internal void AppendTagName(char append)
            {
                AppendTagName(append.ToString());
            }

            internal void AppendAttributeName(string append)
            {
                // might have null chars because we eat in one pass - need to replace with null replacement character
                append = append.Replace(TokeniserState.nullChar, Tokeniser.replacementChar);

                EnsureAttrName();
                if (attrName.Length == 0) {
                    attrNameS = append;
                } else {
                    attrName.Append(append);
                }
            }

            internal void AppendAttributeName(char append)
            {
                EnsureAttrName();
                attrName.Append(append);
            }

            internal void AppendAttributeValue(string append)
            {
                EnsureAttrValue();
                if (attrValue.Length == 0) {
                    attrValueS = append;
                } else {
                    attrValue.Append(append);
                }
            }

            internal void AppendAttributeValue(char append)
            {
                EnsureAttrValue();
                attrValue.Append(append);
            }

            internal void AppendAttributeValue(char[] append)
            {
                EnsureAttrValue();
                attrValue.Append(append);
            }
            
            public void AppendAttributeValue(int[] appendCodepoints) {
                EnsureAttrValue();
                foreach (int codepoint in appendCodepoints) {
                    attrValue.Append(char.ConvertFromUtf32(codepoint));
                }
            }
            
            internal void SetEmptyAttributeValue() {
                hasEmptyAttrValue = true;
            }
            
            private void EnsureAttrName() {
                hasAttrName = true;
                // if on second hit, we'll need to move to the builder
                if (attrNameS != null) {
                    attrName.Append(attrNameS);
                    attrNameS = null;
                }
            }
            
            private void EnsureAttrValue() {
                hasAttrValue = true;
                // if on second hit, we'll need to move to the builder
                if (attrValueS != null) {
                    attrValue.Append(attrValueS);
                    attrValueS = null;
                }
            }
        }

        internal class StartTag : Token.Tag
        {
            public StartTag() : base()
            {
                attributes = new Attributes();
                type = TokenType.StartTag;
            }

            internal StartTag(string name) : this()
            {
                this.tagName = name;
            }

            internal StartTag(string name, Attributes attributes) : this()
            {
                this.tagName = name;
                this.attributes = attributes;
            }

            public override string ToString()
            {
                if (attributes != null && attributes.Count > 0)
                {
                    return "<" + Name() + " " + attributes.ToString() + ">";
                }
                else
                {
                    return "<" + Name() + ">";
                }
            }
        }

        internal class EndTag : Token.Tag
        {
            public EndTag() : base()
            {
                type = TokenType.EndTag;
            }

            internal EndTag(string name) : this()
            {
                this.tagName = name;
            }

            public override string ToString()
            {
                return "</" + Name() + ">";
            }
        }

        internal class Comment : Token
        {
            internal readonly StringBuilder data = new StringBuilder();

            internal bool bogus = false;

            public Comment()
            {
                type = TokenType.Comment;
            }

            internal string GetData()
            {
                return data.ToString();
            }

            public override string ToString()
            {
                return "<!--" + GetData() + "-->";
            }
        }

        internal class Character : Token
        {
            private readonly string data;

            internal Character(string data)
            {
                type = TokenType.Character;
                this.data = data;
            }

            internal string GetData()
            {
                return data;
            }

            public override string ToString()
            {
                return GetData();
            }
        }

        internal class EOF : Token
        {
            public EOF()
            {
                type = TokenType.EOF;
            }
        }

        internal bool IsDoctype()
        {
            return type == TokenType.Doctype;
        }

        internal Token.Doctype AsDoctype()
        {
            return (Token.Doctype)this;
        }

        internal bool IsStartTag()
        {
            return type == TokenType.StartTag;
        }

        internal Token.StartTag AsStartTag()
        {
            return (Token.StartTag)this;
        }

        internal bool IsEndTag()
        {
            return type == TokenType.EndTag;
        }

        internal Token.EndTag AsEndTag()
        {
            return (Token.EndTag)this;
        }

        internal bool IsComment()
        {
            return type == TokenType.Comment;
        }

        internal Token.Comment AsComment()
        {
            return (Token.Comment)this;
        }

        internal bool IsCharacter()
        {
            return type == TokenType.Character;
        }

        internal Token.Character AsCharacter()
        {
            return (Token.Character)this;
        }

        internal bool IsEOF()
        {
            return type == TokenType.EOF;
        }

        
    }
}
