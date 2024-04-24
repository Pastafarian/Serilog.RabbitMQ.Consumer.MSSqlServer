# There is a bug in the serilog-settings-configuration package that prevents the use of the Override property in the MinimumLevel configuration. 
# This is a workaround to set the minimum level to Information. 

https://github.com/serilog/serilog-settings-configuration/issues/332

This blows up. 

"MinimumLevel": {
  "Default": "Information",
  "Override": {
    "Microsoft": "Warning",
    "System": "Error"
    }
  }

 
This works.
"MinimumLevel": "Information",