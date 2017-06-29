Formula Car Telemetry System
========================================================

### What are we?
  
The formula student association consist of approximately 600 universities from all around the world.
Each university is required, under strict regulations, to design and build a formula race car in one year.
At the end of the year, the team can participate in a various competitions around the world.

The telemetry will help the team to detect potential problems and inform the driver about the current state.
For example, if the driver is too aggressive and wasting too much gas, the telemetry’s fuel consumption calculation can tell if the driver can finish the race or will have to calm down a bit.

### How does it work?

The car's network is powered by CAN bus protocol, our system includes digital devices that we're using to conect to this network, convert the network messages, and transmit them to a remote control workstation.
Then, a C# designed software, we will receive the messages and present them in a way that will be easy to the user to supervise the car.

For more explanations, check the presentation that's in the main folder.

### Features

* Graphical objects to present the incoming data, such as analog clocks and a moveable graph.

* Multi-threading abilities to maximize the software performance.

* Saved output and log files to analyze the car and software activity after the race.

* Error detection and alert mechanism, to alert the user in any problem that can occur.

* Dataloss artificial treatment to be able to recover some lost data in case of wireless disconnection.

* Offline option to be able to present old datalog like an online telemetry.

### Installation

This project was built in C# environment at Visual Studio 2015 profession, it's highly recommended to use the same or higher version than this.

The exact instructions on how to download and compile the project can be found in "Instructions.txt" in the project's main folder.

### Thank you

We really hope you can enjoy our project as much as we did.

If you have any questions or suggestions for improvement, feel free to contact:
deanzadok@campus.technion.ac.il
