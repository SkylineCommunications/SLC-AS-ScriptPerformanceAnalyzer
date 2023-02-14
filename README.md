# SLC-AS-ScriptPerformanceAnalyzer
A generic DataMiner Low-Code app to analyze script performance.

## Deployment
To analyze the performance of a script, implement performance logging in the script using the Skyline.DataMiner.Utils.ScriptPerformanceLogger nuget and deploy this package on the DMA.


## Analysis
Run the script as it would normally be run.

After the script is finnished, it will generate a metrics file in C:\Skyline_Data\Metrics with a timestamp and the scriptname as filename.

Open the Script Performance Analyzer Low-Code app and select the file that was generated for your script run in the top table.

The bottom table and the timeline component will now populate with all metrics from the file. Zoom in on the timeline untill you see the blocks representing the method exectutions and their duration. A filter is available on the left if you want to focuss on a specific method or class.

## Notes

* The application requires the [SLC-AS-GQI-Files GQI Data Source](https://github.com/SkylineCommunications/SLC-AS-GQI-Files) to function correctly.
* As the files are only read from the local DMA, the Low-Code app needs to be opened on the DMA where the script ran.
* Currently no cleanup of the metric files is included, these need to be cleaned up manually.
