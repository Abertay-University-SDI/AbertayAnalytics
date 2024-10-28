# Abertay Analytics
Abertay Analytics is a Unity package developed by Gareth Robinson at Abertay University in Dundee.
It is a simplified interface to allow for analytic data gathering in the Unity game engine.
The package provides a unified interface that can send data to a local .csv file or, if set-up, Unity Analytics.

# Installing Abertay Analytics
The simplest way to install the Abertay Analytics package in your Unity project is to click the green `Code` dropdown above and copy the [Web URL](https://github.com/Abertay-University-SDI/AbertayAnalytics.git).
Then, in your Unity project, open the Package Manager window (Window->Package Manager).
In this window there is a '+' dropdown in the top left of the screen that reveals an option to add a new package from a git URL. Paste the link to the repository into this.

If this does *not* work, an alternative method is to instead click the green dropdown above and download a .zip file of the project.
From here, you should be able extract the .zip and copy the "AbertayAnalytics-main" folder into the "Packages" folder of your Unity project.

# Using Abertay Analytics
Abertay Analytics is designed to require minimal set-up.
To include Abertay Analytics in your game, you must add the **Analytics Manager** to your first Scene in your game. The Analytics Manager Game Object handles the various systems of Abertay Analytics.

Once the Analytics Manager is present in your scene it needs to be **initialised**. The easiest way to do this is to tick the **Initialise On Start** checkbox on the Analytics Manager component.
If you would rather do this manually at some other time through a script, you can leave this checkbox empty and instead call one of the `Initialise` functions described below.

Once the analytics system has been initialised, it will remain so until the game is exited. It does not need to be initialised on a per-scene basis.

## Initialisation
If the **Initialise On Start** parameter is enable, the Analytics Manager will initialise the system on startup with default values and will **not** track on a per-user basis.
If you require to track individual Users, you should instead use the `InitialiseWithCustomID` function with **Initialise On Start** disabled.

### `Initialise(string environmentName)`
This function is used to manually initialise the analytics system. It has one optional parameter `environmentName` which allows the developer to specify a name for the current environment.
If using **Unity Analytics** this will enable the developer to sort their data between analytic environments.
In **Abertay Analytics** it can be used to separate data into distinct buckets.

### `InitialiseWithCustomID(string customID, string environmentName, System.Action callback)'
This function is used to manually initialise the analytics system while specifying a unique identifier for the current user. It is most useful for keeping track of an individual user's habits, or for sorting data at a later date.
- **customID** - A string that represents the user.
- **environmentName** - (optional) allows the developer to specify a name for the current environment
- **callback** - (optional) A function to call when the initialisation is complete. (Only really useful for Unity Analytics).

You may want to make use of the **Custom User ID example** scene which provides a simple UI interface for a playtester to enter their name which is then used as their Custom ID.

## Logging Analytic Data
Logging any analytic data that you want to track is done through a single function call: `SendCustomEvent`.
This function will ensure that all enable backends (Abertay and/or Unity) received the correct information as supplied.

### `SendCustomEvent(string eventName, Dictionary<string, object> parameters)`
- `eventName` - A string identifying the type of event. This should be relatively unique and identifiable so that it is useful for sorting. e.g "PlayerJump", "PlayerDeath", "EnemyDeath"
- `parameters` - This `<string, object>` Dictionary contains an optional list of parameters that provide as much, or as little, information about the event as you desire.
  - The `string` is the Key ID for the parameter you want to track. If you want to track the elapsed time on a "PlayerJump" event, you might call this "ElapsedTime".
  - The `object` is a value of any serialisable type. For the "ElapsedTime" example above, this is likely to be of type `float`.
 
## Data Output
If using the editor, the output from the analytic logs is stored in the folder **"Assets/Analytics/Events"**.
If running from a Standalone Build, the output will be contained in an **Analytics** folder in the same location as the .exe.
- Events_<ENVIRONMENT>_CSV
  - A comma-separated variable file that sorts the data into rows and columns. Open this with Excel or equivalent for relatively easy parsing of data.
- Events_<ENVIRONMENT>_JSON
  - A JSON file with all of the events separated into separate JSON objects. It's unlikely you'll need to look at this, but some systems will allow for JSON import. 

# Using the Heatmap

**Coming Soon**
