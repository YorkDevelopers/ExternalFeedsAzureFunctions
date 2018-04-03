To debug locally.

First install the tools using:-
	npm install -g azure-functions-core-tools@core

Then populate your local.settings.json file by :-
	cd into the feeds folder
	func azure functionapp fetch-app-settings FunctionApp20180403104502

To run a timer function
	add a dummy http triggered function
	call the timer function from the dummy function
	press f5,  open the localhost website
	invoice the dummy function eg  http://localhost:7071/api/Function1