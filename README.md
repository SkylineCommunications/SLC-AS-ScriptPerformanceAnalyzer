# SLC-AS-ScriptPerformanceAnalyzer
A generic DataMiner Low-Code app to analyze script performance.

## Deployment
To analyze the performance of a script, 
implement performance logging into the script using the Skyline.DataMiner.Utils.ScriptPerformanceLogger nuget and deploy this Script Performance Analyzer package on the DMA.

## Analysis
Run the script as it would normally be run.

After the script is finished, 
it will generate a metrics file in C:\Skyline_Data\ScriptPerformanceLogger with a timestamp and the scriptname as filename.

Open the Script Performance Analyzer Low-Code app and select the file that was generated for your script execution in the top table.

The bottom table and the timeline component will now populate with all metrics from the file. 
Zoom in on the timeline until you see the blocks representing the method executions and their duration. 
A filter is available on the left if you want to focus on a specific method or class.

## Notes

* The application requires the [SLC-AS-GQI-Files GQI Data Source](https://github.com/SkylineCommunications/SLC-AS-GQI-Files) to function correctly.
* As the files are only read from the local DMA, the Low-Code app needs to be opened on the DMA where the script ran.
* Metric files can be automatically cleaned using [Archive Script Performance Results](#archive-script-performance-results)

![image](https://user-images.githubusercontent.com/110403333/218707260-7598b7d8-f6de-46ab-9d4e-20cc35d1120d.png)

# Archive Script Performance Results
An automation script to prevent the number of files created by the ScriptPerformanceLogger from growing endlessly.

The script will zip all results older than a specified number of days. 
The zip file will roll over after reaching a specified size.

## Deployment
Use the scheduler in DataMiner to create a daily task that runs the script.

![image](Documentation/archive%20general.png)
![image](Documentation/archive%20schedule.png)
![image](Documentation/archive%20action.png)

# Publish Daily Script Performance Results
An automation script to aggregate and push performance results of the last 24 hours to the QAPortal

## Deployment
Use the scheduler in DataMiner to create a daily task that runs the script.

The script has 6 mandatory parameters:

* Portal Link, Client ID and API Key will be provided by the contact at Skyline Communications.
For internal users at Skyline Communications, these 3 values can be a dot (.)
* Domain: a skyline domain or e-mail address
* Agent Name: name of the agent (not the clusterName) containing the performance results
* Project ID: one or multiple project ids separated by comma (,)

## Notes
This script uses the [QAPortalAPI](https://www.nuget.org/packages/Skyline.DataMiner.Utils.QAPortalAPI#readme-body-tab)