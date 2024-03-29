﻿using Sample.Sdk.Data.Constants;

namespace Sample.Sdk.Data.Options
{
    public class DatabaseSettingOptions
    {
        public static string DatabaseSetting = Environment.GetEnvironmentVariable(ConfigVar.DB_CONN_STR)!;
        public string ConnectionString { get; set; } = string.Empty;
    }
}
