using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace XMLToJSONDocDump
{
    /// <summary>
    /// Enum for listing what type of object a MemberSummary is based on the XML file.
    /// </summary>
    public enum MemberType
    {
        /// <summary>
        /// Default.
        /// </summary>
        None = 0,
        /// <summary>
        /// Member is a property.
        /// </summary>
        Property = 1,
        /// <summary>
        /// Member is a type definition.
        /// </summary>
        Type = 2,
        /// <summary>
        /// Member is a field.
        /// </summary>
        Field = 3,
        /// <summary>
        /// Member is a method.
        /// </summary>
        Method = 4
    }

    /// <summary>
    /// A container for the XML comment made on a class or member.
    /// </summary>
    public class MemberSummary
    {
        protected string _name = null;
        protected string _namespacePath = null;
        protected string _description = null;
        protected MemberType _memberType = MemberType.None;

        /// <summary>
        /// The name of the member or class.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// A string composed of the namespace of the declaring class and any parent nested classes. The full "Path" to get to a member in a given namespace.
        /// </summary>
        public string NamespacePath
        {
            get { return _namespacePath; }
            set { _namespacePath = value; }
        }

        /// <summary>
        /// The XML summary description of the member or class.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// The type of member.
        /// </summary>
        public MemberType MemberType
        {
            get { return _memberType; }
            set { _memberType = value; }
        }

        public override string ToString()
        {
            string name = _name == null ? "(NULL)" : _name;
            string ns = _namespacePath == null ? "(NULL)" : _namespacePath;

            return ns + "." + name;
        }
    }

    /// <summary>
    /// Container for the comments made on a method.
    /// </summary>
    public class MethodDefinition : MemberSummary
    {
        protected List<MethodParameter> _parameters = new List<MethodParameter>();

        /// <summary>
        /// The XML comments on the parameters of the method.
        /// </summary>
        public List<MethodParameter> Parameters
        {
            get { return _parameters; }
        }

        /// <summary>
        /// Container for the comments made on a parameter to a method.
        /// </summary>
        public class MethodParameter
        {
            protected string _paramName = null;
            protected string _paramDescription = null;
            protected string _paramType = null;

            /// <summary>
            /// The name of the parameter.
            /// </summary>
            public string ParamName
            {
                get { return _paramName; }
                set { _paramName = value; }
            }

            /// <summary>
            /// The description of the parameter.
            /// </summary>
            public string ParamDescription
            {
                get { return _paramDescription; }
                set { _paramDescription = value; }
            }

            /// <summary>
            /// The type of the parameter.
            /// </summary>
            public string ParamType
            {
                get { return _paramType; }
                set { _paramType = value; }
            }

            public override string ToString()
            {
                string paramType = (_paramType == null) ? "(NULL)" : _paramType;
                string paramName = (_paramName == null) ? "(NULL)" : _paramName;

                return _paramType + " " + _paramName;
            }
        }

        public override string ToString()
        {
            string methodName = (_name == null) ? "(NULL)" : _name;
            string parameters = "";

            for (int x = 0; x < _parameters.Count; x++)
            {
                if (x != _parameters.Count -1)
                {
                    parameters += _parameters[x].ToString() + ", ";
                }
                else
                {
                    parameters += _parameters[x].ToString();
                }
            }

            return methodName + "(" + parameters + ")";
        }
    }

    /// <summary>
    /// Utility class for scraping a XML documentation file and organizing its contents into an array of easily-queryable objects.
    /// </summary>
    public class XMLDocParser
    {
        protected List<MemberSummary> _members = new List<MemberSummary>();
        protected Assembly _assembly = null;

        /// <summary>
        /// All of the comments made on all members in an assembly.
        /// </summary>
        public List<MemberSummary> Members
        {
            get { return _members; }
        }

        /// <summary>
        /// Reads the XML documentation file for the assembly and parses its contents into a list of easil-queryable objects.
        /// </summary>
        /// <param name="assembly">The assembly to get the XML contents for.</param>
        public void GetXMLComments(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentException("assembly");

            _members = new List<MemberSummary>();
            _assembly = assembly;

            string assemblyXMLPath = GetDocumentationXMLFilePath(_assembly);
            if (File.Exists(assemblyXMLPath) == false) throw new Exception("No XML documentation file for the given assembly exists.");

            ReadXMLDoc(assemblyXMLPath);

        }

        /// <summary>
        /// Gets the file path of the XML documentation file for the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get the XML documentation file path for.</param>
        /// <returns></returns>
        protected string GetDocumentationXMLFilePath(Assembly assembly)
        {
            if (assembly == null) return null;

            string assemblyPath = assembly.CodeBase.Replace("file:///", "");
            string assemblyXMLPath = assemblyPath.Substring(0, assemblyPath.Length - 4) + ".xml";

            return assemblyXMLPath;
        }

        /// <summary>
        /// Reads the assembly's XML documentation and parses out the names and summary descriptions of each member.
        /// </summary>
        /// <param name="xmlDocFilePath"></param>
        protected void ReadXMLDoc(string xmlDocFilePath)
        {
            if (xmlDocFilePath == null || File.Exists(xmlDocFilePath) == false) return;

            using (XmlReader reader = XmlTextReader.Create(xmlDocFilePath))
            {
                while (reader.Read() == true)
                {
                    if (reader.Name != "member" || reader.NodeType == XmlNodeType.EndElement) continue;

                    string name = reader.GetAttribute("name");
                    string summary = null;

                    while (reader.Read() == true)
                    {
                        if (reader.Name == "summary")
                        {
                            reader.Read();
                            summary = reader.Value.Trim();
                            break;
                        }
                    }

                    MemberSummary info = ProcessXMLData(name, summary, reader);
                    if (info != null) _members.Add(info);
                }
            }
        }

        /// <summary>
        /// Turns an entry from a XML documentation file into a MemberSummary object.
        /// </summary>
        /// <param name="name">The value of the "Name" attribute from the XML documentation.</param>
        /// <param name="summary">The summary string from the XML documentation.</param>
        /// <param name="reader">The XML reader that is reading the XML file.</param>
        /// <returns></returns>
        protected MemberSummary ProcessXMLData(string name, string summary, XmlReader reader)
        {
            if (name == null || summary == null || name.Length == 0) return null;

            MemberSummary info = null;
            string firstChar = name.Substring(0, 1).ToLower();             

            if (firstChar == "p" || firstChar == "t" || firstChar == "f") //property, field, or type
            {
                int lastDot = name.LastIndexOf(".");
                string description = summary;
                string namespacePath = name.Substring(2, lastDot - 2); //the name of the member always starts with a type prefix and a colon, get the rest of the string after the prefix
                string memberName = name.Substring(lastDot + 1);

                info = new MemberSummary();
                info.Name = memberName;
                info.Description = description;
                info.NamespacePath = namespacePath;

                if (firstChar == "p") info.MemberType = MemberType.Property;
                else if (firstChar == "t") info.MemberType = MemberType.Type;
                else if (firstChar == "f") info.MemberType = MemberType.Field;
            }
            else if (firstChar == "m") //method
            {
                int lastDot = name.LastIndexOf(".");
                int firstParenthesis = name.IndexOf("(");

                string description = summary;
                string namespacePath = null;
                string memberName = null;

                if (firstParenthesis == -1) //no parenthsis on the method signature in the xml documentation if it has no parameters
                {
                    namespacePath = name.Substring(2, lastDot - 2); //the name of the member always starts with a type prefix and a colon, get the rest of the string after the prefix
                    memberName = name.Substring(lastDot + 1);
                }
                else
                {
                    string withoutParams = name.Substring(0, firstParenthesis); //cut off the parameter string
                    lastDot = withoutParams.LastIndexOf("."); //get the last dot in the parameterless name (otherwise the namespaces of the parameters get used as the last dot)

                    namespacePath = withoutParams.Substring(2, lastDot - 2); //the name of the member always starts with a type prefix and a colon, get the rest of the string after the prefix
                    memberName = name.Substring(namespacePath.Length + 3); //2 for the prefix, 1 for the last dot
                }

                info = new MethodDefinition();
                info.Name = memberName;
                info.Description = description;
                info.NamespacePath = namespacePath;
                info.MemberType = MemberType.Method;

                return ReadMethodParameters(info, reader);
            }

            return info;
        }

        /// <summary>
        /// Walks through the parameters for a method in the xml documentation file, parses them, and adds them as MethodDefinition.MethodParameter to the methodInfo's parameter list.
        /// </summary>
        /// <param name="methodInfo">The data from the XML file that pertains to the method and its signature.</param>
        /// <param name="reader">The XML reader that is reading the </param>
        /// <returns></returns>
        protected MemberSummary ReadMethodParameters(MemberSummary methodInfo, XmlReader reader)
        {
            if (methodInfo == null || reader == null) return null;

            string originalName = methodInfo.Name;
            string[] parameterTypes = null;

            if (methodInfo.Name != null)
            {
                int signatureStart = methodInfo.Name.IndexOf("(");
                if (signatureStart != -1)
                {
                    methodInfo.Name = methodInfo.Name.Substring(0, signatureStart);

                    string paramTypesString = originalName.Substring(signatureStart + 1, originalName.Length - signatureStart - 2); //skip over first parenthsis, last parenthsis
                    parameterTypes = paramTypesString.Split(',');

                    if (parameterTypes.Length == 0) parameterTypes = null;
                }

                if (methodInfo.Name.Contains("#ctor") == true) //its a constructor - we dont want the #ctor in the name because it is ugly and will confuse some developers/consumers of documentation
                {
                    int finalSegmentStart = methodInfo.NamespacePath.LastIndexOf(".");
                    if (finalSegmentStart != -1) methodInfo.Name = methodInfo.NamespacePath.Substring(finalSegmentStart + 1, methodInfo.NamespacePath.Length - finalSegmentStart - 1);
                }
            }

            int paramNumber = 0;
            while (reader.Read() == true)
            {
                if (reader.Name == "member" && reader.NodeType == XmlNodeType.EndElement) break; //reached the end of the method defintion
                if (reader.Name != "param" || reader.NodeType == XmlNodeType.EndElement) continue;               

                string name = reader.GetAttribute("name");
                string summary = null;
  
                reader.Read();
                summary = reader.Value.Trim();

                MethodDefinition.MethodParameter curParam = new MethodDefinition.MethodParameter();
                curParam.ParamName = name;
                curParam.ParamDescription = summary;

                if (parameterTypes != null && paramNumber <= parameterTypes.Length)
                {
                    curParam.ParamType = parameterTypes[paramNumber]; //we got the types from the method signature and the params are in the same order as the types, so we can match them up.
                }

                (methodInfo as MethodDefinition).Parameters.Add(curParam);
                paramNumber++;
            }

            return methodInfo; 
        }
    }
}
