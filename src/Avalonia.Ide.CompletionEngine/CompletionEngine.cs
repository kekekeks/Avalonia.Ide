using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Avalonia.Ide.CompletionEngine
{
    public class CompletionEngine
    {
        class MetadataHelper
        {
            private Metadata _metadata;
            public Metadata Metadata => _metadata;
            public Dictionary<string, string> Aliases { get; private set; }

            Dictionary<string, MetadataType> _types;
            private string _currentAssemblyName;

            public void SetMetadata(Metadata metadata, string xml, string currentAssemblyName = null)
            {
                var aliases = GetNamespaceAliases(xml);

                //Check if metadata and aliases can be reused
                if (_metadata == metadata && Aliases != null && _types != null && currentAssemblyName == _currentAssemblyName)
                {
                    if (aliases.Count == Aliases.Count)
                    {
                        bool mismatch = false;
                        foreach (var alias in aliases)
                        {
                            if (!Aliases.ContainsKey(alias.Key) || Aliases[alias.Key] != alias.Value)
                            {
                                mismatch = true;
                                break;
                            }
                        }

                        if (!mismatch)
                            return;
                    }
                }
                Aliases = aliases;
                _metadata = metadata;
                _types = null;
                _currentAssemblyName = currentAssemblyName;

                var types = new Dictionary<string, MetadataType>();
                foreach (var alias in Aliases.Concat(new[] { new KeyValuePair<string, string>("", "") }))
                {
                    Dictionary<string, MetadataType> ns;

                    string aliasValue = alias.Value ?? "";

                    if (!string.IsNullOrEmpty(_currentAssemblyName) && aliasValue.StartsWith("clr-namespace:") && !aliasValue.Contains(";assembly="))
                        aliasValue = $"{aliasValue};assembly={_currentAssemblyName}";

                    if (!metadata.Namespaces.TryGetValue(aliasValue, out ns))
                        continue;

                    var prefix = alias.Key.Length == 0 ? "" : (alias.Key + ":");
                    foreach (var type in ns.Values)
                        types[prefix + type.Name] = type;
                }

                _types = types;

            }

            public IEnumerable<KeyValuePair<string, MetadataType>> FilterTypes(string prefix, bool withAttachedPropertiesOnly = false, bool markupExtensionsOnly = false, bool staticGettersOnly = false, bool xamlDirectiveOnly = false)
            {
                prefix = prefix ?? "";

                var e = _types
                    .Where(t => t.Value.IsXamlDirective == xamlDirectiveOnly && t.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                if (withAttachedPropertiesOnly)
                    e = e.Where(t => t.Value.HasAttachedProperties);
                if (markupExtensionsOnly)
                    e = e.Where(t => t.Value.IsMarkupExtension);
                if (staticGettersOnly)
                    e = e.Where(t => t.Value.HasStaticGetProperties);

                return e;
            }

            public IEnumerable<string> FilterTypeNames(string prefix, bool withAttachedPropertiesOnly = false, bool markupExtensionsOnly = false, bool staticGettersOnly = false, bool xamlDirectiveOnly = false)
            {
                return FilterTypes(prefix, withAttachedPropertiesOnly, markupExtensionsOnly, staticGettersOnly, xamlDirectiveOnly).Select(s => s.Key);
            }

            public MetadataType LookupType(string name)
            {
                MetadataType rv;
                _types.TryGetValue(name, out rv);
                return rv;
            }

            public IEnumerable<string> FilterPropertyNames(string typeName, string propName,
                bool? attached,
                bool staticGetter = false)
            {
                var t = LookupType(typeName);
                propName = propName ?? "";
                if (t == null)
                    return new string[0];

                var e = t.Properties.Where(p => p.Name.StartsWith(propName, StringComparison.OrdinalIgnoreCase));

                if (attached.HasValue)
                    e = e.Where(p => p.IsAttached == attached);
                if (staticGetter)
                    e = e.Where(p => p.IsStatic && p.HasGetter);
                else
                    e = e.Where(p => !p.IsStatic);

                return e.Select(p => p.Name);
            }

            public MetadataProperty LookupProperty(string typeName, string propName)
                => LookupType(typeName)?.Properties?.FirstOrDefault(p => p.Name == propName);
        }

        MetadataHelper _helper = new MetadataHelper();

        static Dictionary<string, string> GetNamespaceAliases(string xml)
        {
            var rv = new Dictionary<string, string>();
            try
            {
                var xmlRdr = XmlReader.Create(new StringReader(xml));
                bool result = true;
                while (result && xmlRdr.NodeType != XmlNodeType.Element)
                {
                    try
                    {
                        result = xmlRdr.Read();
                    }
                    catch
                    {
                        if (xmlRdr.NodeType != XmlNodeType.Element) result = false;
                    }
                }

                if (result)
                {
                    for (var c = 0; c < xmlRdr.AttributeCount; c++)
                    {
                        xmlRdr.MoveToAttribute(c);
                        var ns = xmlRdr.Name;
                        if (ns != "xmlns" && !ns.StartsWith("xmlns:"))
                            continue;
                        ns = ns == "xmlns" ? "" : ns.Substring(6);
                        rv[ns] = xmlRdr.Value;
                    }
                }
            }
            catch
            {
                //
            }
            if (!rv.ContainsKey(""))
                rv[""] = Utils.AvaloniaNamespace;
            return rv;
        }

        public CompletionSet GetCompletions(Metadata metadata, string text, int pos, string currentAssemblyName = null)
        {
            _helper.SetMetadata(metadata, text, currentAssemblyName);

            if (_helper.Metadata == null)
                return null;

            if (text.Length == 0 || pos == 0)
                return null;
            text = text.Substring(0, pos);
            var state = XmlParser.Parse(text);

            var completions = new List<Completion>();


            var curStart = state.CurrentValueStart ?? 0;

            if (state.State == XmlParser.ParserState.StartElement)
            {
                var tagName = state.TagName;
                if (tagName.StartsWith("/"))
                {
                    if (text.Length < 2)
                        return null;
                    var closingState = XmlParser.Parse(text.Substring(0, text.Length - 2));

                    var name = closingState.GetParentTagName(0);
                    if (name == null)
                        return null;
                    completions.Add(new Completion("/" + name + ">", CompletionKind.Class));
                }
                else if (tagName.Contains("."))
                {
                    var dotPos = tagName.IndexOf(".");
                    var typeName = tagName.Substring(0, dotPos);
                    var compName = tagName.Substring(dotPos + 1);
                    curStart = curStart + dotPos + 1;

                    var sameType = state.GetParentTagName(1) == typeName;

                    completions.AddRange(_helper.FilterPropertyNames(typeName, compName, sameType ? (bool?)null : true)
                        .Select(p => new Completion(p, sameType ? CompletionKind.Property : CompletionKind.AttachedProperty)));
                }
                else
                    completions.AddRange(_helper.FilterTypeNames(tagName).Select(x => new Completion(x, CompletionKind.Class)));
            }
            else if (state.State == XmlParser.ParserState.InsideElement ||
                     state.State == XmlParser.ParserState.StartAttribute)
            {

                if (state.State == XmlParser.ParserState.InsideElement)
                    curStart = pos; //Force completion to be started from current cursor position

                if (state.AttributeName?.Contains(".") == true)
                {
                    var dotPos = state.AttributeName.IndexOf('.');
                    curStart += dotPos + 1;
                    var split = state.AttributeName.Split(new[] { '.' }, 2);
                    completions.AddRange(_helper.FilterPropertyNames(split[0], split[1], true)
                        .Select(x => new Completion(x, x + "=\"\"", x, CompletionKind.AttachedProperty, x.Length + 2)));
                }
                else
                {
                    completions.AddRange(_helper.FilterPropertyNames(state.TagName, state.AttributeName, false)
                        .Select(x => new Completion(x, x + "=\"\"", x, CompletionKind.Property, x.Length + 2)));

                    var targetType = _helper.LookupType(state.TagName);
                    completions.AddRange(
                        _helper.FilterTypes(state.AttributeName, xamlDirectiveOnly: true)
                            .Where(t => t.Value.IsValidForXamlContextFunc?.Invoke(currentAssemblyName, targetType, null) ?? true)
                            .Select(v => new Completion(v.Key, v.Key + "=\"\"", v.Key, CompletionKind.Class, v.Key.Length + 2)));

                    //if (targetType?.IsAvaloniaObjectType == true)
                    //    completions.AddRange(
                    //        _helper.FilterTypes(state.AttributeName, true)
                    //            .Select(v => new Completion(v.Key,
                    //            v.Key + (v.Value.Properties.Count > 0 ? "." : "=\"\""),
                    //            v.Key, CompletionKind.Class,
                    //            v.Value.Properties.Count > 0 ? -1 : v.Key.Length + 2)));
                    if (targetType?.IsAvaloniaObjectType == true)
                        completions.AddRange(
                            _helper.FilterTypeNames(state.AttributeName, withAttachedPropertiesOnly: true)
                                .Select(v => new Completion(v, v + ".", v, CompletionKind.Class)));
                }
            }
            else if (state.State == XmlParser.ParserState.AttributeValue)
            {
                MetadataProperty prop;
                if (state.AttributeName.Contains("."))
                {
                    //Attached property
                    var split = state.AttributeName.Split('.');
                    prop = _helper.LookupProperty(split[0], split[1]);
                }
                else
                    prop = _helper.LookupProperty(state.TagName, state.AttributeName);

                //Markup extension, ignore everything else
                if (state.AttributeValue.StartsWith("{"))
                {
                    curStart = state.CurrentValueStart.Value +
                               BuildCompletionsForMarkupExtension(prop, completions,
                                   text.Substring(state.CurrentValueStart.Value), currentAssemblyName);
                }
                else
                {
                    if (prop?.Type?.HasHintValues == true)
                    {
                        var search = text.Substring(state.CurrentValueStart.Value);
                        if (prop.Type.IsCompositeValue)
                        {
                            var last = search.Split(' ', ',').LastOrDefault();
                            curStart = curStart + search.Length - last?.Length ?? 0;
                            search = last;
                        }

                        completions.AddRange(GetHintCompletions(prop.Type, search, currentAssemblyName));
                    }
                    else if (prop?.Type?.Name == typeof(Type).FullName)
                    {
                        completions.AddRange(_helper.FilterTypeNames(state.AttributeValue).Select(x => new Completion(x, x, x, CompletionKind.Class)));
                    }
                    else if (state.AttributeName == "xmlns" || state.AttributeName.Contains("xmlns:"))
                    {
                        if (state.AttributeValue.StartsWith("clr-namespace:"))
                            completions.AddRange(
                                metadata.Namespaces.Keys.Where(v => v.StartsWith(state.AttributeValue))
                                    .Select(v => new Completion(v.Substring("clr-namespace:".Length), v, v, CompletionKind.Namespace)));
                        else
                        {
                            if ("clr-namespace:".StartsWith(state.AttributeValue))
                                completions.Add(new Completion("clr-namespace:", CompletionKind.Namespace));
                            completions.AddRange(
                                metadata.Namespaces.Keys.Where(
                                    v =>
                                        v.StartsWith(state.AttributeValue) &&
                                        !v.StartsWith("clr-namespace"))
                                    .Select(v => new Completion(v, CompletionKind.Namespace)));
                        }
                    }
                    else if (state.AttributeName.EndsWith(":Class"))
                    {

                        if (_helper.Aliases.TryGetValue(state.AttributeName.Replace(":Class", ""), out var ns) && ns == Utils.Xaml2006Namespace)
                        {
                            var asmKey = $";assembly={currentAssemblyName}";
                            var fullClassNames = _helper.Metadata.Namespaces.Where(v => v.Key.EndsWith(asmKey))
                                                                            .SelectMany(v => v.Value.Values.Where(t => t.IsAvaloniaObjectType))
                                                                            .Select(v => v.FullName);
                            completions.AddRange(
                                   fullClassNames
                                    .Where(v => v.StartsWith(state.AttributeValue))
                                    .Select(v => new Completion(v, CompletionKind.Class)));
                        }
                    }
                }
            }

            if (completions.Count != 0)
                return new CompletionSet() { Completions = completions, StartPosition = curStart };
            return null;
        }

        public IEnumerable<string> FilterHintValues(MetadataType type, string entered, string currentAssemblyName)
        {
            entered = entered ?? "";

            if (type == null)
                yield break;

            if (!string.IsNullOrEmpty(currentAssemblyName) && type.XamlContextHintValuesFunc != null)
            {
                foreach (var v in type.XamlContextHintValuesFunc(currentAssemblyName, type, null).Where(v => v.StartsWith(entered, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return v;
                }
            }

            foreach (var v in type.HintValues.Where(v => v.StartsWith(entered, StringComparison.OrdinalIgnoreCase)))
            {
                yield return v;
            }
        }

        List<Completion> GetHintCompletions(MetadataType type, string entered, string currentAssemblyName = null)
        {
            var kind = GetCompletionKindForHintValues(type);

            return FilterHintValues(type, entered, currentAssemblyName).Select(val => new Completion(val, kind)).ToList();
        }

        int BuildCompletionsForMarkupExtension(MetadataProperty property, List<Completion> completions, string data, string currentAssemblyName)
        {
            int? forcedStart = null;
            var ext = MarkupExtensionParser.Parse(data);

            var transformedName = (ext.ElementName ?? "").Trim();
            if (_helper.LookupType(transformedName)?.IsMarkupExtension != true)
                transformedName += "Extension";

            if (ext.State == MarkupExtensionParser.ParserStateType.StartElement)
                completions.AddRange(_helper.FilterTypeNames(ext.ElementName, markupExtensionsOnly: true)
                    .Select(t => t.EndsWith("Extension") ? t.Substring(0, t.Length - "Extension".Length) : t)
                    .Select(t => new Completion(t, CompletionKind.MarkupExtension)));
            if (ext.State == MarkupExtensionParser.ParserStateType.StartAttribute ||
                ext.State == MarkupExtensionParser.ParserStateType.InsideElement)
            {
                if (ext.State == MarkupExtensionParser.ParserStateType.InsideElement)
                    forcedStart = data.Length;

                completions.AddRange(_helper.FilterPropertyNames(transformedName, ext.AttributeName ?? "", false)
                    .Select(x => new Completion(x, x + "=", x, CompletionKind.Property)));

                var attribName = ext.AttributeName ?? "";
                var t = _helper.LookupType(transformedName);

                bool ctorArgument = ext.AttributesCount == 0;
                //skip ctor hints when some property is already set
                if (t != null && t.IsMarkupExtension && t.SupportCtorArgument != MetadataTypeCtorArgument.None && ctorArgument)
                {
                    if (t.SupportCtorArgument == MetadataTypeCtorArgument.HintValues)
                    {
                        if (t.HasHintValues)
                        {
                            completions.AddRange(GetHintCompletions(t, attribName));
                        }
                    }
                    else if (attribName.Contains("."))
                    {
                        if (t.SupportCtorArgument != MetadataTypeCtorArgument.Type)
                        {
                            var split = attribName.Split('.');
                            var type = split[0];
                            var prop = split[1];

                            var mType = _helper.LookupType(type);
                            if (mType != null && t.SupportCtorArgument == MetadataTypeCtorArgument.HintValues)
                            {
                                var hints = FilterHintValues(mType, prop, currentAssemblyName);
                                completions.AddRange(hints.Select(x => new Completion(x, $"{type}.{x}", x, GetCompletionKindForHintValues(mType))));
                            }

                            var props = _helper.FilterPropertyNames(type, prop, false, staticGetter: true);
                            completions.AddRange(props.Select(x => new Completion(x, $"{type}.{x}", x, CompletionKind.StaticProperty)));
                        }
                    }
                    else
                    {
                        var types = _helper.FilterTypeNames(attribName,
                            staticGettersOnly: t.SupportCtorArgument == MetadataTypeCtorArgument.Object);

                        completions.AddRange(types.Select(x => new Completion(x, x, x, CompletionKind.Class)));

                        if (property?.Type?.HasHintValues == true)
                        {
                            completions.Add(new Completion(property.Type.Name, property.Type.Name + ".", property.Type.Name, CompletionKind.Class));
                        }
                    }
                }
            }
            if (ext.State == MarkupExtensionParser.ParserStateType.AttributeValue
                || ext.State == MarkupExtensionParser.ParserStateType.BeforeAttributeValue)
            {
                var prop = _helper.LookupProperty(transformedName, ext.AttributeName);
                if (prop?.Type?.HasHintValues == true)
                {
                    var start = data.Substring(ext.CurrentValueStart);
                    completions.AddRange(GetHintCompletions(prop.Type, start));
                }
            }

            return forcedStart ?? ext.CurrentValueStart;
        }

        public static bool ShouldTriggerCompletionListOn(char typedChar)
        {
            return char.IsLetterOrDigit(typedChar) || typedChar == '/' || typedChar == '<'
                || typedChar == ' ' || typedChar == '.' || typedChar == ':';
        }

        public static CompletionKind GetCompletionKindForHintValues(MetadataType type)
            => type.IsEnum ? CompletionKind.Enum : CompletionKind.StaticProperty;
    }
}
