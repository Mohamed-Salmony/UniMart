using Microsoft.Extensions.Localization;
using System.Reflection;
using System.Text.Json;

namespace UniMart_App.Services
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly JsonDocument? _localizationData;
        private readonly string _culture;

        public JsonStringLocalizer()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith("localization.json"));

            if (resourcePath != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourcePath);
                if (stream != null)
                {
                    _localizationData = JsonDocument.Parse(stream);
                }
            }
            else
            {
                // Fallback to file system
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "localization.json");
                if (File.Exists(filePath))
                {
                    var jsonString = File.ReadAllText(filePath);
                    _localizationData = JsonDocument.Parse(jsonString);
                }
            }

            _culture = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var value = GetString(name);
                return new LocalizedString(name, value ?? name, value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = GetString(name);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, format == null);
            }
        }

        private string? GetString(string name)
        {
            if (_localizationData == null)
                return null;

            try
            {
                // Split the name by dots to navigate the JSON structure
                var parts = name.Split('.');
                if (parts.Length < 2)
                    return null;

                var section = parts[0];
                var key = parts[1];

                // Try to get the value for the current culture
                var cultureElement = _localizationData.RootElement.GetProperty(_culture);
                var sectionElement = cultureElement.GetProperty(section);
                return sectionElement.GetProperty(key).GetString();
            }
            catch
            {
                // If any part of the path doesn't exist, return null
                return null;
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var result = new List<LocalizedString>();

            if (_localizationData == null)
                return result;

            try
            {
                var cultureElement = _localizationData.RootElement.GetProperty(_culture);
                foreach (var section in cultureElement.EnumerateObject())
                {
                    foreach (var key in section.Value.EnumerateObject())
                    {
                        var name = $"{section.Name}.{key.Name}";
                        var value = key.Value.GetString();
                        result.Add(new LocalizedString(name, value ?? name, value == null));
                    }
                }
            }
            catch
            {
                // If any error occurs, return the empty list
            }

            return result;
        }
    }

    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource)
        {
            return new JsonStringLocalizer();
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return new JsonStringLocalizer();
        }
    }
}
