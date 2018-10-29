using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hypar.Functions
{
    /// <summary>
    /// Generate function code from a HyparConfig.
    /// </summary>
    public class CodeGen
    {
        private HyparConfig _config;

        /// <summary>
        /// Construct a CodeGen.
        /// </summary>
        /// <param name="config"></param>
        public CodeGen(HyparConfig config)
        {
            this._config = config;
        }

        /// <summary>
        /// Emit C# code representing a function adapter, a function, and input and output classes.
        /// If a function has already been generated previously in the specified location, it will not be generated again.
        /// </summary>
        /// <param name="outputDirectory"></param>
        public void EmitCSharp(string outputDirectory)
        {
            var inputClassName = "Input";
            var outputClassName = "Output";
            var functionName = SanitizedName(this._config.FunctionId, true);

            EmitFunctionHarness(functionName, inputClassName, outputClassName, outputDirectory);
            if (!File.Exists(Path.Combine(outputDirectory, $"{functionName}.cs")))
            {
                EmitFunction(functionName, inputClassName, outputClassName, outputDirectory);
            }
            EmitClass(this._config.Inputs, inputClassName, outputDirectory);
            EmitClass(this._config.Outputs, outputClassName, outputDirectory);
        }

        private string SanitizedName(string name, bool camelCase)
        {   
            var splits = name.Split(new[]{' ','-'});
            
            var cleanName = "";

            foreach(var split in splits)
            {
                if(camelCase)
                {
                    cleanName += split.First().ToString().ToUpper() + split.Substring(1);
                }
                else
                {
                    cleanName += split.First().ToString().ToLower() + cleanName.Substring(1);
                }
            }
            return cleanName;
        }

        private string TypeName(HyparParameterType t)
        {
            switch (t)
            {
                case HyparParameterType.Location:
                    return "Polygon";
                case HyparParameterType.Number:
                    return "double";
                case HyparParameterType.Point:
                    return "Vector3";
				case HyparParameterType.Range:
                    return "double";
                default:
                    return string.Empty;
            }
        }

        private void EmitFunctionHarness(string functionName, string inputClassName, string outputClassName, string outputDirectory)
        {
var code = $@"// This code was generated by Hypar.
// Edits to this code will be overwritten the next time code generation occurs.
// DO NOT EDIT THIS FILE.
using Amazon.Lambda.Core;
using Hypar.Geometry;
using Hypar.Elements;
using Hypar.GeoJSON;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace Hypar.Functions
{{
	public class Function
	{{
    	public {outputClassName} Handler({inputClassName} input, ILambdaContext context)
    	{{
			var f = new {functionName}();
			return f.Execute(input);
    	}}
  	}}
}}";
            File.WriteAllText(Path.Combine(outputDirectory, $"Function.g.cs"), code);
        }

        private void EmitFunction(string functionName, string inputClassName, string outputClassName, string outputDirectory)
        {
            var code = $@"using Hypar.Elements;

namespace Hypar.Functions
{{
  	public class {functionName}
	{{
		public {outputClassName} Execute({inputClassName} input)
		{{
			/// Your code here.
		}}
  	}}
}}";
            File.WriteAllText(Path.Combine(outputDirectory, $"{functionName}.cs"), code);
        }

        private void EmitClass(Dictionary<string, InputOutputBase> fields, string className, string outputDirectory)
        {
            var propSb = new StringBuilder();
            var args = new List<string>();
            var assignSb = new StringBuilder();

            foreach (var input in fields)
            {
                var propName = SanitizedName(input.Key, true);
                var argName = SanitizedName(input.Key, false);
                var typeName = TypeName(input.Value.Type);
				var jsonName = input.Key;

                propSb.AppendLine($"\t\t[JsonProperty(\"{jsonName}\")]\n\t\tpublic {typeName} {propName} {{get;set;}}");
				args.Add($"{typeName} {argName}");
                assignSb.AppendLine($"\t\t\tthis.{propName} = {argName};");
            }

var code = $@"// This code was generated by Hypar.
// Edits to this code will be overwritten the next time code generation occurs.
// DO NOT EDIT THIS FILE.
using Hypar.Elements;
using Hypar.Geometry;
using Newtonsoft.Json;

namespace Hypar.Functions
{{
	public class {className}
	{{
{propSb.ToString()}
		[JsonIgnore]
		public Model Model{{get;set;}}

		public {className}({string.Join(", ", args)})
		{{
{assignSb.ToString()}
		}}
	}}
}}";
            File.WriteAllText(Path.Combine(outputDirectory, $"{className}.g.cs"), code);
        }
    }
}