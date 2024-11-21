using System.Globalization;
using System.IO;
using System.Text;
using SoundFlowSystem.Data;
using UnityEngine;

namespace SoundFlowSystem.Libraries
{
    public static class SoundsCollectionConstantGenerator
    {
        public static void GenerateClassFile(SoundData[] soundsData)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("namespace App.SoundFlowSystem.Libraries");
            sb.AppendLine("{");
            sb.AppendLine("    public static class SoundsCollectionConstants");
            sb.AppendLine("    {");
            
            foreach (var soundData in soundsData)
            {
                var key = ConvertToPascalCase(soundData.Key);
                
                sb.AppendLine($"        public static readonly string {key} = \"{soundData.Key}\";");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            File.WriteAllText(Application.dataPath + "/Pray4Us/Scripts/App/SoundFlowSystem/Libraries/SoundsCollectionConstants.cs", sb.ToString());
        }
        
        private static string ConvertToPascalCase(string input)
        {
            var parts = input.Split('_');
            var result = new StringBuilder();
            
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    result.Append(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(part.ToLower()));
                }
            }

            return result.ToString();
        }
    }
}