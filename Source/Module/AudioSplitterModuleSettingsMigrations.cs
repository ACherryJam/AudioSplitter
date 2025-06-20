using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.AudioSplitter.Audio;

namespace Celeste.Mod.AudioSplitter.Module
{
    using Migrator = Func<AudioSplitterModuleSettings, AudioSplitterModuleSettings>;

    public static class AudioSplitterModuleSettingsMigrations
    {
        private readonly static Dictionary<AudioSplitterModuleSettings.Version, Migrator> migrations = new() {
            { AudioSplitterModuleSettings.Version.Initial, NullDeviceNameFixMigration },
        };

        public static AudioSplitterModuleSettings Migrate(AudioSplitterModuleSettings settings)
        {
            try
            {
                var newSettings = settings;
                while (migrations.ContainsKey(newSettings.SettingsVersion))
                {
                    newSettings = migrations[settings.SettingsVersion].Invoke(newSettings);
                    Logger.Debug(nameof(AudioSplitterModule), $"Performed settings migration to {newSettings.SettingsVersion}");
                }

                Logger.Info(nameof(AudioSplitterModule), $"Migrated settings to {newSettings.SettingsVersion}");
                return newSettings;
            } catch (Exception e)
            {
                Logger.Error(nameof(AudioSplitterModule), $"Failed to perform migrations from version {settings}");
                Logger.LogDetailed(e);
                return settings;
            }
        }

        private static AudioSplitterModuleSettings NullDeviceNameFixMigration(AudioSplitterModuleSettings settings)
        {
            // Replace null device names with new default for default device
            static OutputDeviceInfo UpdateName(OutputDeviceInfo info)
            {
                if (info.Name == null)
                    info.Name = OutputDeviceInfo.DefaultDevice.Name;
                return info;
            }

            settings.SFXOutputDevice = UpdateName(settings.SFXOutputDevice);
            settings.MusicOutputDevice = UpdateName(settings.MusicOutputDevice);
            settings.AudioOutputDevice = UpdateName(settings.AudioOutputDevice);

            settings.SettingsVersion = AudioSplitterModuleSettings.Version.NullDeviceNameFix;
            return settings;
        }
    }
}
