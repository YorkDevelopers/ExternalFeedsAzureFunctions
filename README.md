# External Feeds using Azure Functions
This set of tools is used to gather events from a number of external sources which are then included on the yorkDevelopers website.

The collection of events is a two stage process

1.  The events are collected from an external source and then written as yml in our common event format to the _data folder
2.   The merge files process then takes each of these yml files and combines them into a single yml file called Events.yml 


![alt text](https://github.com/YorkDevelopers/ExternalFeedsAzureFunctions/blob/master/Documentation/Overview.png "Overview")
                 
