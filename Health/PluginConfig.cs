using System;

namespace RealismModSync.Health
{
    /// <summary>
    /// Helper class to access RealismMod's PluginConfig via reflection
    /// </summary>
    public static class PluginConfig
    {
        private static Type _pluginConfigType;
        private static object _enableMedicalLogging;

        static PluginConfig()
        {
            try
            {
                _pluginConfigType = Type.GetType("RealismMod.PluginConfig, RealismMod");
                
                if (_pluginConfigType != null)
                {
                    _enableMedicalLogging = _pluginConfigType.GetProperty("EnableMedicalLogging")?.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                Plugin.REAL_Logger.LogWarning($"Could not access RealismMod.PluginConfig: {ex.Message}");
            }
        }

        public static bool EnableMedicalLogging
        {
            get
            {
                try
                {
                    if (_enableMedicalLogging == null)
                        return false;

                    var valueProperty = _enableMedicalLogging.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        return (bool)valueProperty.GetValue(_enableMedicalLogging);
                    }
                }
                catch
                {
                    // Ignore
                }
                return false;
            }
        }
    }
}
