using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XMLToJSONDocDump
{
    /// <summary>
    /// Utility class for exporting a specialized JSON dump of an object model. Produces a JSON-typed object example and a table of all the comments included in the C# assembly for classes and their members.
    /// </summary>
    public class JSONDocDump
    {
        protected XMLDocParser _parser = new XMLDocParser();
        protected List<Type> _types = null;
        protected StringBuilder _builder = null;
        protected List<Type> _numericTypes = new List<Type>() { typeof(UInt16), typeof(UInt32), typeof(UInt64), typeof(Int16), typeof(Int32), typeof(Int64), typeof(Single), typeof(Double), typeof(Decimal), typeof(Byte), typeof(SByte) };

        /// <summary>
        /// Writes a dump file of the given assembly to the given path.
        /// </summary>
        /// <param name="assemblyToDump">The assembly to create the dump file for.</param>
        /// <param name="outputFilePath">The location of the text file to write.</param>
        public void CreateDumpFile(Assembly assemblyToDump, string outputFilePath)
        {
            if (assemblyToDump == null) throw new ArgumentException("assemblyToDump");
            if (outputFilePath == null) throw new ArgumentException("outputFilePath");

            _parser = new XMLDocParser();
            _parser.GetXMLComments(assemblyToDump); //read the doc file and get all of the comments on each member and organize them
            _builder = new StringBuilder();
            _types = new List<Type>(assemblyToDump.GetTypes());

            foreach (Type type in _types) //make documentation for each type after reflecting on the assembly
            {
                if (ShouldIncludeType(type) == false) continue;
                MakeDocumentationForType(type); //writes to the string builder (_builder) for each type
            }

            //dump all the text in the destination file
            File.WriteAllText(outputFilePath, _builder.ToString(), System.Text.Encoding.Unicode);
        }

        /// <summary>
        /// Whether or not to include a member in the JSON object description.
        /// </summary>
        /// <param name="reflectionMemberInfo">The reflected member info of the member to include or not.</param>
        /// <returns></returns>
        protected virtual bool ShouldIncludeMember(MemberInfo reflectionMemberInfo)
        {
            if (reflectionMemberInfo.DeclaringType.IsEnum == true && reflectionMemberInfo.Name == "value__") return false;
            return true;
        }

        /// <summary>
        /// Whether or not to include a member in the table of summary descriptions.
        /// </summary>
        /// <param name="summary">The summary description from the XML doc file.</param>
        /// <param name="reflectionMemberInfo">The reflected member info of the member to include or not.</param>
        /// <returns></returns>
        protected virtual bool ShouldIncludeInDocTable(MemberSummary summary, MemberInfo reflectionMemberInfo)
        {
            if (reflectionMemberInfo.DeclaringType.IsEnum == true && reflectionMemberInfo.Name == "value__") return false;
            return true;
        }

        /// <summary>
        /// Whether or not a type should be included in the dump.
        /// </summary>
        /// <param name="type">The type to include or not.</param>
        /// <returns></returns>
        protected virtual bool ShouldIncludeType(Type type)
        {
            if (type.FullName.Contains("<>c__DisplayClass") == true) return false;
            return true;
        }

        /// <summary>
        /// Writes all the lines of documentation needed for the JSON example and properties table for an object of the given type to the internal string builder, _builder.
        /// </summary>
        /// <param name="t">The type to produce documentation for.</param>
        protected void MakeDocumentationForType(Type t)
        {
            if (t == null) return;

            List<MemberSummary> matches = GetMemberSummariesForType(t); //find all the summary descriptions from the XML file for the given type and its members
            List<MemberInfo> reflectedMembers = GetReflectedMembersForType(t); //get all the public fields and properties belonging to the type

            if (matches == null || reflectedMembers == null || (matches.Count == 0 && reflectedMembers.Count == 0)) return;

            string typeStringToMatch = (t.IsNested == true) ?  t.FullName.Replace("+", ".") : t.FullName;
            MemberSummary typeSummary = matches.Where(item => item.NamespacePath + "." + item.Name == typeStringToMatch && item.MemberType == MemberType.Type).FirstOrDefault(); //get the doc summary entry for the type definition itself
            string header = t.Name + " - " + ((typeSummary == null) ? "??" : typeSummary.Description);

            _builder.Append(header);
            _builder.AppendLine();
            _builder.Append(MakeJSONSampleForType(t, reflectedMembers));
            _builder.AppendLine("\n");
            _builder.Append(MakeDocumentationTableForType(t, matches, reflectedMembers));
            _builder.AppendLine("---------------------------------------------------------------------------------------");
        }

        /// <summary>
        /// Makes a JSON sample object with JavaScript types instead of values for each property.
        /// </summary>
        /// <param name="t">The type to make the JSON sampe of.</param>
        /// <param name="reflectedMembers">The public fields and properties of the member.</param>
        /// <returns></returns>
        protected StringBuilder MakeJSONSampleForType(Type t, List<MemberInfo> reflectedMembers)
        {
            if (t == null || reflectedMembers == null) return null;

            StringBuilder jsonSample = new StringBuilder();
            jsonSample.AppendLine("{"); 

            for(int x = 0; x < reflectedMembers.Count; x++)
            {
                MemberInfo reflectedMember = reflectedMembers[x];
                if (ShouldIncludeMember(reflectedMember) == false) continue;

                Type itemType = (reflectedMember is PropertyInfo) ? (reflectedMember as PropertyInfo).PropertyType : (reflectedMember as FieldInfo).FieldType; //its always a field info or prop info

                string typeEquivalent = GetJavaScriptTypeEquivalent(itemType);
                if (typeEquivalent == null) continue; //no equivalent type, skip

                string entry = GetJSONSampleLine(reflectedMember, typeEquivalent);

                if (x != reflectedMembers.Count - 1) //no lagging comma on the last entry in the JSON sample
                { 
                    entry += ",";
                }

                jsonSample.AppendLine(entry);
            }

            jsonSample.AppendLine("}");

            return jsonSample;
        }

        /// <summary>
        /// Overridable function for generating the line for the example JSON for a given member.
        /// </summary>
        /// <param name="reflectedMember">The relfected member of a class.</param>
        /// <param name="typeEquivalent">The JavaScript type equivalent for the member.</param>
        /// <returns></returns>
        protected virtual string GetJSONSampleLine(MemberInfo reflectedMember, string typeEquivalent)
        {
            if (reflectedMember.DeclaringType.IsEnum == true)
            {
                Type enumType = System.Enum.GetUnderlyingType(reflectedMember.DeclaringType);
                Array enumValues = Enum.GetValues(reflectedMember.DeclaringType);
                object enumValue = null;
                object enumMember = null;

                for (int x = 0; x < enumValues.Length; x++)
                {
                    enumMember = enumValues.GetValue(x);
                    enumValue = System.Convert.ChangeType(enumMember, enumType);

                    if (enumMember.ToString() == reflectedMember.Name) break;                    
                }

                return "\t\"" + reflectedMember.Name + "\":" + enumValue.ToString();
            }
            else if(reflectedMember is FieldInfo && (reflectedMember as FieldInfo).IsLiteral == true) //if it "IsLiteral" that means it's a const and we want to enter in the value for the const instead of it's type
            {
                object constantValue = (reflectedMember as FieldInfo).GetRawConstantValue();
                string entry = "\t\"" + reflectedMember.Name + "\":";

                if (constantValue is String)
                {
                    entry += "\"" + constantValue.ToString() + "\"";
                }
                else
                {
                    entry += constantValue.ToString();
                }

                return entry;
            }
            else
            {
                return "\t\"" + reflectedMember.Name + "\":<" + typeEquivalent + ">";
            }

        }

        /// <summary>
        /// Makes a table of documentation pulled from the XML C# comments in the assembly for the given type.
        /// </summary>
        /// <param name="t">The type to make the documentation table for.</param>
        /// <param name="summaryDescriptions">The list of all the XML comments for the members of the type.</param>
        /// <param name="reflectedMembers">The list of all the reflected properties and fields of the type.</param>
        /// <returns></returns>
        protected StringBuilder MakeDocumentationTableForType(Type t, List<MemberSummary> summaryDescriptions, List<MemberInfo> reflectedMembers)
        {
            if (t == null || summaryDescriptions == null || reflectedMembers == null) return null;

            StringBuilder tableBuilder = new StringBuilder();
            foreach (MemberInfo reflectedMember in reflectedMembers)
            {
                MemberSummary match = summaryDescriptions.FirstOrDefault(item => item.Name == reflectedMember.Name);
                if (ShouldIncludeInDocTable(match, reflectedMember) == false) continue;

                Type itemType = (reflectedMember is PropertyInfo) ? (reflectedMember as PropertyInfo).PropertyType : (reflectedMember as FieldInfo).FieldType;
                string summary = (match == null) ? "??" : match.Description;
                string jsType = GetJavaScriptTypeEquivalent(itemType);
                if (jsType == null) jsType = "??";

                string tableLine = GetTableLine(reflectedMember, summary, jsType);
                tableBuilder.AppendLine(tableLine);

            }

            return tableBuilder;
        }

        /// <summary>
        /// Overrideable function for making a line in the documentation table for a given member of a type.
        /// </summary>
        /// <param name="reflectedMember">The field or property info object describing the member we're writing a line for.</param>
        /// <param name="summary">The XML comment summary made for the line.</param>
        /// <param name="jsType">The JavaScript equivalent type for the member.</param>
        /// <returns></returns>
        protected virtual string GetTableLine(MemberInfo reflectedMember, string summary, string jsType)
        {
            return reflectedMember.Name + " <" + jsType + ">: " + summary;
        }

        /// <summary>
        /// Queries the list of members in the assembly and pulls out the summary for the type and all of it's XML documented members.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected List<MemberSummary> GetMemberSummariesForType(Type t, bool recursive = true)
        {
            if (t == null) return null;

            string namespacePath = t.FullName.Replace("+", ".");

            List<MemberSummary> matches = new List<MemberSummary>(_parser.Members.Where(member => member.NamespacePath.StartsWith(namespacePath) || member.NamespacePath + "." + member.Name == namespacePath).ToArray());

            if (recursive == true) //we need some recursion to get all the documentation of a derived type (the documentation is not repeated for derived types)
            {
                foreach (Type type in _types)
                {
                    if (t.IsSubclassOf(type) == true)
                    {
                        List<MemberSummary> moreMatches = GetMemberSummariesForType(type, false);
                        if (moreMatches != null) matches.AddRange(moreMatches);
                    }
                }
            }

            return matches;
        }

        /// <summary>
        /// Gets all the public fields and properties belonging to a given type.
        /// </summary>
        /// <param name="t">The type to get the public fields and properties of.</param>
        /// <returns></returns>
        protected List<MemberInfo> GetReflectedMembersForType(Type t)
        {
            if (t == null) return null;

            List<MemberInfo> reflectedMembers = new List<MemberInfo>();
            reflectedMembers.AddRange(t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
            reflectedMembers.AddRange(t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));

            return reflectedMembers;
        }

        /// <summary>
        /// Gets the JavaScript type equivalent to a C# type.
        /// </summary>
        /// <param name="t">The type to get the JavaScript type for.</param>
        /// <returns></returns>
        protected string GetJavaScriptTypeEquivalent(Type t)
        {
            if (t == null) return null;

            if (t.IsPrimitive)
            {
                if (_numericTypes.Contains(t) == true)
                {
                    return "Number";
                }
                else if (t == typeof(Boolean))
                {
                    return "Boolean";
                }
                else
                {
                    return null;
                }
            }
            else if (t == typeof(String) || t == typeof(Char))
            {
                return "String";
            }
            else if (t.GetInterface("IList") != null || t == typeof(Array))
            {
                Type elementType = null;

                if (t.GetInterface("IList") != null)
                {
                    elementType = t.GetGenericArguments()[0];
                }
                else
                {
                    elementType = t.GetElementType();
                }

                if (elementType != null)
                {
                    return "Array of " + GetJavaScriptTypeEquivalent(elementType);
                }
                else
                {
                    return "Array";
                }
            }
            else if (t.IsClass)
            {
                if (t.IsNested == true)
                {
                    return t.FullName.Replace(t.Namespace, "").Replace("+", ".").Substring(1) + " Object";
                }
                else
                {
                    return t.Name + " Object";
                }
            }
            else if (t.IsEnum)
            {
                return "Number"; //enums can only ever be numeric types
            }
            else
            {
                return null;
            }
        }
    }
}
