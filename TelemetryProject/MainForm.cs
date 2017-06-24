using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace TelemetryProject
{
    public partial class MainForm : Form
    {
        //Public constants
        public const int TIMER_INTERVAL = 50;
        public const double TIME_DELTA = 0.05;
        public const float MINIMAL_DEGREE = -120;
        public const float MAXIMAL_DEGREE = 120;
        public const double MAX_FUEL_CAPACITY = 5.0;
        public const int INJECTORS_NUMBER = 1;
        public const double INJECTION_FLOW = 10.75;
        public const int GRAPH_LENGTH = 72000;
        public const int ALARM_TIMER_INTERVAL = 500;
        public const int HIGH_RPM = 11500;
        public const int HIGH_TEMPERATURE = 105;


        //Global variables
        System.Windows.Forms.Timer refreshTimer;
        string[] enduranceLogFields;
        TextFieldParser parser;
        double fuelCapacityAtStart;
        double fuelCapacity;
        int fuelBarHeight;
        int fuelBarHeightLocation;
        double timeFromStart;
        OpenFileDialog openCSVDialog = new OpenFileDialog();
        string csvPath;

        int timerCounter;
        int errorsCounter;

        //rpm clock
        float rpmClockDegree;
        Bitmap rpmIndicatorBmp;

        //temperature clock
        float tempClockDegree;
        Bitmap tempIndicatorBmp;

        //speed clock
        float speedClockDegree;
        Bitmap speedIndicatorBmp;

        //Graph
        Pen tempPen, rpmPen;
        int oldTempValue, newTempValue;
        int oldRpmValue, newRpmValue;
        PictureBox graphBmpPictureBox;
        Image graphBmp;
        int graphLength;
        bool scrollBarInUse;

        //Online option
        SerialPort serialPort;
        System.IO.StreamWriter logFile;
        String messageBuffer;
        Queue<string> dataFromSerialPort;
        Thread receivingThread;
        Thread alarmThread;
        System.Windows.Forms.Timer alarmTimer;
        string[] alarmMessages;
        String warningToPresent;
        int noMessagesCounter;
        StringBuilder onlineCsv;

        //Endurance calculation information
        int enduranceLengthInSeconds;
        double suggestedConsumptionPerSecond;
        double suggestedFuelSpent;
        double fuelSpent;

        //Presentation variables
        int rpmToPresent;
        int tempToPresent;
        int speedToPresent;
        string gearLevelToPresent;
        double batteryVoltageToPresent;
        double mapPressureToPresent;
        int airTemperatureToPresent;
        double lambdaToPresent;
        double antiRollToPresent;
        double accelerationToPresent;
        double fuelOpenTimeToPresent;

        public MainForm()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //Counter for timer
            timerCounter = 0;

            //rpm presentation initialize
            rpmClockDegree = MINIMAL_DEGREE;
            RpmLabel.Text = (((rpmClockDegree + MAXIMAL_DEGREE) / (MAXIMAL_DEGREE - MINIMAL_DEGREE)) * 12000).ToString();
            RpmLabel.BackColor = Color.Transparent;
            RpmLabel.Parent = RpmCPictureBox;
            rpmIndicatorBmp = new Bitmap(TelemetryProject.Properties.Resources.indicator);
            RpmCPictureBox.Image = RotateImg(rpmIndicatorBmp, rpmClockDegree, 77);

            //temp clock initialize
            tempClockDegree = MINIMAL_DEGREE;
            tempClockLabel.Text = ((tempClockDegree / 4) + 80).ToString();
            tempClockLabel.BackColor = Color.Transparent;
            tempClockLabel.Parent = tempCPictureBox;
            tempClockLabel.BringToFront();
            tempIndicatorBmp = new Bitmap(TelemetryProject.Properties.Resources.indicator);
            tempCPictureBox.Image = RotateImg(tempIndicatorBmp, tempClockDegree, 55);

            //speed clock initialize
            speedClockDegree = MINIMAL_DEGREE;
            speedClockLabel.Text = ((speedClockDegree / 2) + 60).ToString();
            speedClockLabel.BackColor = Color.Transparent;
            speedClockLabel.Parent = speedCPictureBox;
            speedIndicatorBmp = new Bitmap(TelemetryProject.Properties.Resources.indicator);
            speedCPictureBox.Image = RotateImg(speedIndicatorBmp, speedClockDegree, 55);

            //fuel presentation initialize
            fuelCapacityAtStart = MAX_FUEL_CAPACITY;
            fuelCapacity = MAX_FUEL_CAPACITY;
            fuelBarHeight = fuelCapacityPictureBox.Height;
            fuelBarHeightLocation = fuelCapacityPictureBox.Location.Y;
            fuelDiffBarPictureBox.BringToFront();

            //time label initialize
            timeFromStart = 0.0;
            timeLabel.Text = "00:00";

            //temperature graph initialize
            graphLength = 0;
            graphBmpPictureBox = new PictureBox();
            graphBmpPictureBox.Size = new Size(GRAPH_LENGTH, graphPictureBox.Height);
            graphBmpPictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.graphBmpPictureBox_MouseMove);
            graphBmpPictureBox.MouseLeave += new System.EventHandler(this.graphBmpPictureBox_MouseLeave);
            graphPictureBox.Controls.Add(graphBmpPictureBox);
            graphBmp = new Bitmap(graphBmpPictureBox.Width, graphBmpPictureBox.Height);
            graphBmpPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.graphBmpPictureBox_Paint);
            tempPen = new Pen(Color.LightSeaGreen, 1);
            rpmPen = new Pen(Color.Gold, 1);
            scrollBarInUse = false;

            //initialize timer
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = TIMER_INTERVAL;
            refreshTimer.Tick += new EventHandler(HandleTimer);

            //Initialize alarm timer
            alarmTimer = new System.Windows.Forms.Timer();
            alarmTimer.Interval = ALARM_TIMER_INTERVAL;
            alarmTimer.Tick += new EventHandler(HandleWarningTimer);

            //temporary solution for fixing form dimensions
            this.Width += 570;
            this.Height += 200;
            onlineOptionPanel.Location = csvFilePanel.Location;

            //endurance initialize
            enduranceLengthInSeconds = 0;
            enduranceTimePanel.Location = new Point(FuelPanel.Location.X, setFuelCapacityButton.Location.Y);
            fuelCapacitySetPanel.Location = new Point(FuelPanel.Location.X, setFuelCapacityButton.Location.Y);

            //Presentation initialize
            rpmToPresent = 0;
            tempToPresent = 50;
            speedToPresent = 0;
            gearLevelToPresent = "N";
            batteryVoltageToPresent = 0;
            mapPressureToPresent = 0;
            airTemperatureToPresent = 0;
            lambdaToPresent = 0;
            antiRollToPresent = 90;
            accelerationToPresent = 0;
            fuelOpenTimeToPresent = 0;

            //Presentation labels initialize
            gearLabel.Text = gearLevelToPresent;
            batteryLabel.Text = batteryVoltageToPresent.ToString();
            mapLabel.Text = mapPressureToPresent.ToString();
            airTemperatureLabel.Text = airTemperatureToPresent.ToString();
            lambdaLabel.Text = lambdaToPresent.ToString();
            antiRollLabel.Text = antiRollToPresent.ToString();
            accelerationLabel.Text = accelerationToPresent.ToString();

            messageBuffer = "";
            dataFromSerialPort = new Queue<string>();

            warningToPresent = "";
            alarmMessages = new string[] {"","","","","",""};
            noMessagesCounter = 0;
            errorsCounter = 0;
        }

        private void HandleTimer(object source, EventArgs e)
        {
            //update time label
            timeFromStart += TIME_DELTA;
            int timeMinutes = (Convert.ToInt32(timeFromStart) / 60);
            int timeSeconds = (Convert.ToInt32(timeFromStart) % 60);

            bool errorsNumberDisplayed = false;

            if (timeMinutes < 10) {

                timeLabel.Text = "0";
            }
            timeLabel.Text += timeMinutes.ToString() + ":";
            if (timeSeconds < 10) {

                timeLabel.Text += "0";
            }
            timeLabel.Text += timeSeconds.ToString();

            if (onlineOptionButton.Checked) { //ONLINE - update presented data from serial port

                if (timerCounter > 0) {

                    oldRpmValue = rpmToPresent;
                    oldTempValue = tempToPresent;
                }

                for (int i=0;i<5;i++) { //read 5 messages each timer iteration

                    bufferCountLabel.Text = dataFromSerialPort.Count.ToString();
                    bufferSizeLabel.Text = ((dataFromSerialPort.Count) * 60).ToString();

                    if (dataFromSerialPort.Count > 0) {

                        noMessagesCounter = 0;
                        alarmMessages[2] = "";

                        String messageToDecipher = dataFromSerialPort.Dequeue();
                        lastMessageLabel.Text = messageToDecipher;
                        try
                        {
                            if (messageToDecipher.Substring(0, 8) == "0CFFF048")
                            { //The message is PE1

                                rpmToPresent = Convert.ToInt32(convertMessageHexToNumber(messageToDecipher.Substring(9, 4), 1));
                                //fuelOpenTimeToPresent = convertMessageHexToNumber(messageToDecipher.Substring(13, 4), 0.1);

                                if (rpmToPresent > HIGH_RPM)
                                {
                                    alarmMessages[5] = "WARNING! High RPM: " + rpmToPresent;
                                } else {
                                    alarmMessages[5] = "";
                                }

                                logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Read a message - PE1: " + messageToDecipher);
                            }
                            else if (messageToDecipher.Substring(0, 8) == "0CFFF148")
                            { //The message is PE2

                                mapPressureToPresent = convertMessageHexToNumber(messageToDecipher.Substring(13, 4), 0.01);
                                lambdaToPresent = convertMessageHexToNumber(messageToDecipher.Substring(17, 4), 0.01);
                                speedToPresent = Convert.ToInt32(convertMessageHexToNumber(messageToDecipher.Substring(13, 4), 0.01));

                                logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Read a message - PE2: " + messageToDecipher);
                            }
                            else if (messageToDecipher.Substring(0, 8) == "0CFFF348")
                            { //The message is PE4

                                gearLevelToPresent = convertGearVoltagetoLevel(convertMessageHexToNumber(messageToDecipher.Substring(9, 4), 0.001));

                                logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Read a message - PE4: " + messageToDecipher);

                            }
                            else if (messageToDecipher.Substring(0, 8) == "0CFFF548")
                            { //The message is PE6

                                batteryVoltageToPresent = convertMessageHexToNumber(messageToDecipher.Substring(9, 4), 0.01);
                                airTemperatureToPresent = Convert.ToInt32(convertMessageHexToNumber(messageToDecipher.Substring(13, 4), 0.1));
                                tempToPresent = Convert.ToInt32(convertMessageHexToNumber(messageToDecipher.Substring(17, 4), 0.1));

                                if (tempToPresent > HIGH_TEMPERATURE)
                                {
                                    alarmMessages[4] = "WARNING! High temperature: " + tempToPresent;
                                } else {
                                    alarmMessages[4] = "";
                                }

                                logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Read a message - PE6: " + messageToDecipher);
                            }
                            else if (messageToDecipher.Substring(0, 8) == "0CFFA001") { //The message is Pi-innovo01

                                fuelCapacity = fuelCapacityAtStart - (convertMessageHexToNumber(messageToDecipher.Substring(13, 4), 1) / 1000.0);

                                //speedToPresent = Convert.ToInt32(convertMessageHexToNumber(messageToDecipher.Substring(13, 4), 0.0111));

                                logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Read a message - Pi1: " + messageToDecipher);
                            }

                            antiRollToPresent = 90;
                            accelerationToPresent = 0;
                        }
                        catch (Exception ex)
                        {
                            logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Failed to read message: "+ messageToDecipher + ". Error: " + ex.Message);
                            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Failed to read message: " + messageToDecipher + ". Error: " + ex.Message);
                        }
                    } else {

                        noMessagesCounter += refreshTimer.Interval;
                        //wait two seconds until error display
                        if (noMessagesCounter > 2000)
                        {
                            alarmMessages[2] = "ERROR! No messages to display!";
                        }
                    }
                    //display each second
                    if ((timeFromStart % 1) < 0.05 && errorsNumberDisplayed == false)
                    {
                        logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Number of corrupted messages: " + errorsCounter + ". average per second: " + ((double)errorsCounter /(double)timeFromStart));
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Number of corrupted messages: " + errorsCounter + ". average per second: " + ((double)errorsCounter / (double)timeFromStart));
                        errorsNumberDisplayed = true;
                    }
                }
                //Fuel capacity calculation - moved to Pi-innovo
                //fuelCapacity -= ((Convert.ToDouble(rpmToPresent) * TIME_DELTA * fuelOpenTimeToPresent * INJECTORS_NUMBER * INJECTION_FLOW) / (3600000 * 0.77 * 60));

                String exactTime = timeLabel.Text;
                if (!((timeFromStart % 1).ToString().Contains(".")))
                {
                    exactTime += "." + (timeFromStart % 1).ToString();
                } else
                {
                    exactTime += ((timeFromStart % 1).ToString() + "00").Substring(1, 3);
                }
                onlineCsv.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", timeLabel.Text, rpmToPresent, airTemperatureToPresent, tempToPresent, fuelCapacity, mapPressureToPresent, batteryVoltageToPresent, lambdaToPresent, speedToPresent, gearLevelToPresent)); ///////

            } else { //OFLINE - update presented data from csv file

                //load data from csv file
                if (timerCounter > 0) {

                    oldRpmValue = Convert.ToInt32(Convert.ToDouble(enduranceLogFields[1]));
                    oldTempValue = Convert.ToInt32(Convert.ToDouble(enduranceLogFields[3]));
                }
                enduranceLogFields = parser.ReadFields();
                rpmToPresent = Convert.ToInt32(Convert.ToDouble(enduranceLogFields[1]));
                tempToPresent = Convert.ToInt32(Convert.ToDouble(enduranceLogFields[3]));
                speedToPresent = Convert.ToInt32(Convert.ToDouble(enduranceLogFields[5]));
                gearLevelToPresent = "N";
                batteryVoltageToPresent = Convert.ToDouble(enduranceLogFields[6]);
                mapPressureToPresent = Convert.ToDouble(enduranceLogFields[5]);
                airTemperatureToPresent = Convert.ToInt32(Convert.ToDouble(enduranceLogFields[2]));
                lambdaToPresent = Convert.ToDouble(enduranceLogFields[33]);
                antiRollToPresent = 90;
                accelerationToPresent = 0;
                fuelOpenTimeToPresent = Convert.ToDouble(enduranceLogFields[7]);

                //Fuel capacity calculation
                fuelCapacity -= ((Convert.ToDouble(rpmToPresent) * TIME_DELTA * fuelOpenTimeToPresent * INJECTORS_NUMBER * INJECTION_FLOW) / (3600000 * 0.77 * 60));
            }

            //Rpm clock
            rpmClockDegree = Convert.ToInt32((rpmToPresent - 6000) / 50);
            RpmCPictureBox.Image = RotateImg(rpmIndicatorBmp, rpmClockDegree, 77);
            RpmLabel.Text = Convert.ToInt32(rpmToPresent).ToString();

            //Temprature clock
            tempClockDegree = Convert.ToInt32((tempToPresent - 80) * 4);
            tempCPictureBox.Image = RotateImg(tempIndicatorBmp, tempClockDegree, 55);
            tempClockLabel.Text = Convert.ToInt32(tempToPresent).ToString();

            //Speed clock
            speedClockDegree = Convert.ToInt32((speedToPresent - 60) * 2);
            speedCPictureBox.Image = RotateImg(speedIndicatorBmp, speedClockDegree, 55);
            speedClockLabel.Text = Convert.ToInt32(speedToPresent).ToString();

            //Graph
            graphLength++;
            if (timerCounter == 0) {

                oldTempValue = tempToPresent;
                oldRpmValue = rpmToPresent;
            }
            newTempValue = tempToPresent;
            newRpmValue = rpmToPresent;

            graphBmpPictureBox.Invalidate();
            graphBmpPictureBox.Image = graphBmp;

            timerCounter++;
            if (graphLength > graphPictureBox.Width) {

                graphScrollBar.Enabled = true;
                graphScrollBar.Maximum++;

                if (scrollBarInUse == false) {

                    graphBmpPictureBox.Location = new Point(graphBmpPictureBox.Location.X - 1, graphBmpPictureBox.Location.Y);
                    graphScrollBar.Value++;
                }

            }

            fuelCapacityLabel.Text = fuelCapacity.ToString();

            if (!(fuelCapacityLabel.Text.Contains(".")))
            {
                fuelCapacityLabel.Text += ".";
            }
            fuelCapacityLabel.Text += "00";

            if (fuelCapacityLabel.Text.Length > 4)
            {
                fuelCapacityLabel.Text = fuelCapacityLabel.Text.Substring(0, 4);
            }

            fuelCapacityLabel.Text += " L";

            if (fuelCapacityPictureBox.Height > 0) { //update fuel bar

                fuelCapacityPictureBox.Height = Convert.ToInt32((fuelCapacity / 5) * fuelBarHeight);
                fuelCapacityPictureBox.Location = new Point(fuelCapacityPictureBox.Location.X, fuelBarHeightLocation + (fuelBarHeight - Convert.ToInt32((fuelCapacity / 5) * fuelBarHeight)));
            }

            if (fuelCapacityPictureBox.Height >= 160) {//update the fuel bar color

                fuelCapacityPictureBox.BackColor = Color.Lime;
            } else if (fuelCapacityPictureBox.Height < 160 && fuelCapacityPictureBox.Height >= 120) {

                fuelCapacityPictureBox.BackColor = Color.GreenYellow;
            } else if (fuelCapacityPictureBox.Height < 120 && fuelCapacityPictureBox.Height >= 80) {

                fuelCapacityPictureBox.BackColor = Color.Yellow;
            } else if (fuelCapacityPictureBox.Height < 80 && fuelCapacityPictureBox.Height >= 40) {

                fuelCapacityPictureBox.BackColor = Color.Orange;
            } else {

                fuelCapacityPictureBox.BackColor = Color.Red;
            }

            //Fuel Consumption
            if (enduranceLengthInSeconds > 0)
            {
                suggestedFuelSpent = suggestedConsumptionPerSecond * timeFromStart;
                fuelSpent = fuelCapacityAtStart - fuelCapacity;
                if (suggestedFuelSpent > fuelSpent)
                {

                    fuelDiffAmountLabel.Text = (suggestedFuelSpent - fuelSpent).ToString().Substring(0, 5);
                    fuelDiffSignLabel.Text = "+";

                    int newFuelConsumptionBarHeight = Convert.ToInt32((suggestedFuelSpent - fuelSpent) * 500);
                    if (newFuelConsumptionBarHeight <= ((fuelDiffPanel.Height - fuelDiffBarPictureBox.Height) / 2))
                    {

                        fuelDiffAmountPictureBox.Height = newFuelConsumptionBarHeight;
                        fuelDiffAmountPictureBox.Location = new Point(fuelDiffAmountPictureBox.Location.X, fuelDiffBarPictureBox.Location.Y - fuelDiffAmountPictureBox.Height);

                    }
                    fuelDiffAmountPictureBox.BackColor = Color.Lime;
                }
                else
                {

                    fuelDiffAmountLabel.Text = (fuelSpent - suggestedFuelSpent).ToString().Substring(0, 5);
                    fuelDiffSignLabel.Text = "-";

                    int newFuelConsumptionBarHeight = Convert.ToInt32((fuelSpent - suggestedFuelSpent) * 500);
                    if (newFuelConsumptionBarHeight <= ((fuelDiffPanel.Height - fuelDiffBarPictureBox.Height) / 2))
                    {

                        fuelDiffAmountPictureBox.Height = newFuelConsumptionBarHeight;
                        fuelDiffAmountPictureBox.Location = new Point(fuelDiffAmountPictureBox.Location.X, fuelDiffBarPictureBox.Location.Y + fuelDiffBarPictureBox.Height);
                    }
                    fuelDiffAmountPictureBox.BackColor = Color.OrangeRed;
                }
            }
            
            //Gear Level
            gearLabel.Text = gearLevelToPresent.ToString();

            //Battery Voltage
            batteryLabel.Text = batteryVoltageToPresent.ToString();

            //MAP Pressure
            mapLabel.Text = mapPressureToPresent.ToString();

            //Air temperature
            airTemperatureLabel.Text = airTemperatureToPresent.ToString();

            //Lambda
            lambdaLabel.Text = lambdaToPresent.ToString();

            //Anti Roll Degree
            antiRollLabel.Text = antiRollToPresent.ToString();

            //Acceleration
            accelerationLabel.Text = accelerationToPresent.ToString();
        }

        private void receiveDataFromSerialPort()
        {
            while (true) {

                try {

                    String receivedDataFromSerialPort = serialPort.ReadExisting();

                    for (int i = 0; i < receivedDataFromSerialPort.Length; i++) {

                        if (receivedDataFromSerialPort[i] == 'T') {

                            //Check that the current message is not corrupted
                            if (messageBuffer.Length == 30 && messageBuffer[8] == '8') {

                                if (dataFromSerialPort.Count > 3000) { //buffer reached limit

                                    logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": ERROR - Messages buffer reached limit!");
                                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": ERROR - Messages buffer reached limit!");

                                    dataFromSerialPort.Clear();
                                }

                                dataFromSerialPort.Enqueue(messageBuffer);
                                logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Message added: " + messageBuffer);
                                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Message added: " + messageBuffer);
                            }
                            else if (messageBuffer.Length > 0) {

                                errorsCounter++;
                                logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": ERROR - Corrupted message detected: " + messageBuffer);
                                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": ERROR - Corrupted message detected: " + messageBuffer);
                            }
                            messageBuffer = "";
                        } else {

                            messageBuffer += receivedDataFromSerialPort[i];
                        }
                    }

                    alarmMessages[3] = "";
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Failed to receive messages. Error: " + ex.Message + ". Message buffer: " + messageBuffer);
                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Failed to receive messages. Error: " + ex.Message + ". Message buffer: " + messageBuffer);
                    alarmMessages[3] = "ERROR! " + ex.Message;
                }
            }
        }

        private void alarmPresentation()
        {
            while (true)
            {
                int i = 0;

                do
                {
                    warningToPresent = alarmMessages[i];
                    i++;
                } while (i < alarmMessages.Length && warningToPresent == "");

                Thread.Sleep(2000); //wait two seconds to check for the error's update
            }
        }

        private void HandleWarningTimer(object source, EventArgs e)
        {
            alarmLabel.Text = warningToPresent;

            if (alarmLabel.Visible == true) {

                alarmLabel.Visible = false;
            } else {

                alarmLabel.Visible = true;
            }
        }

        /***************************************************************************************************
         *                                            Events                                               *
         ***************************************************************************************************/
        /* Event: startButton_Click
        * 
        * the event will be called when the user press the play button.
        * the event will recognize if the user decided to perform online or offline presentation and will 
        * take care of the rest so the presentation will start.
        */
        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (onlineOptionButton.Checked)
                {  //ONLINE - present data received from serial port

                    //Log file
                    String desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    logFile = new StreamWriter(desktopPath + @"\log " + DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".txt");
                    logFile.WriteLine("**********************************************************************************");
                    logFile.WriteLine("******************************* Telemetry Log File *******************************");
                    logFile.WriteLine("**********************************************************************************");

                    //initialize serial port
                    serialPort = new SerialPort(serialPortTextBox.Text);
                    serialPort.BaudRate = 115200;
                    serialPort.DataBits = 8;

                    serialPort.Open();

                    serialPortTextBox.Enabled = false;

                    ThreadStart receivingThreadStart = new ThreadStart(receiveDataFromSerialPort);
                    receivingThread = new Thread(receivingThreadStart);
                    receivingThread.Start();

                    ThreadStart alarmThreadStart = new ThreadStart(alarmPresentation);
                    alarmThread = new Thread(alarmThreadStart);
                    alarmThread.Start();

                    logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Online Telemetry Started!");

                    onlineCsv = new StringBuilder();
                    onlineCsv.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", "Time", "RPM", "Air Temp", "Temp", "Fuel Capacity", "MAP", "Battery", "Lambda", "Speed", "Gear"));

                    refreshTimer.Start(); //START
                    alarmTimer.Start();
                }
                else //OFFLINE - present data from csv file
                {
                    if (csvPath == null)
                    {
                        MessageBox.Show("Please upload a CSV data log file");
                    }
                    else
                    {
                        //initialize CSV file
                        parser = new TextFieldParser(csvPath);
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");
                        //read only first line
                        enduranceLogFields = parser.ReadFields();

                        refreshTimer.Start(); //START
                    }
                }

                //switch buttons state
                stopButton.Enabled = true;
                openCSVButton.Enabled = false;
                onlineOptionButton.Enabled = false;
                csvOptionButton.Enabled = false;
                startButton.Enabled = false;
                setEnduranceButton.Visible = false;
                setFuelCapacityButton.Visible = false;

                //initialize other staff
                fuelCapacityAtStart = fuelCapacity;
                timeFromStart = 0.0;
                timeLabel.Text = "00:00";
                suggestedConsumptionPerSecond = fuelCapacity / (double)enduranceLengthInSeconds;
                graphScrollBar.Maximum = 0;
                graphScrollBar.Value = 0;
            }
            catch (Exception ex)
            {
                if (onlineOptionButton.Checked) //ONLINE problem
                {
                    logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": telemetry failed to start. Error: " + ex.Message);
                    logFile.WriteLine("**********************************************************************************");
                    logFile.Close();
                }
                MessageBox.Show(ex.Message);
            }
        }
        /* Event: stopButton_Click
        * 
        * the event will be called when the user press the stop button.
        * the event will recognize if the user stoped online or offline presentation and will take care 
        * of the rest so the presentation will stop currectly.
        */
        private void stopButton_Click(object sender, EventArgs e)
        {
            //Stop timers
            refreshTimer.Stop();
            alarmTimer.Stop();

            //reset graph
            graphBmp = new Bitmap(graphBmpPictureBox.Width, graphBmpPictureBox.Height);
            graphScrollBar.Value = 0;
            graphScrollBar.Maximum = 0;

            timerCounter = 0;

            //switch buttons state
            startButton.Enabled = true;
            openCSVButton.Enabled = true;
            onlineOptionButton.Enabled = true;
            csvOptionButton.Enabled = true;
            stopButton.Enabled = false;
            setEnduranceButton.Visible = true;
            setFuelCapacityButton.Visible = true;

            if (onlineOptionButton.Checked)
            {
                try
                {
                    receivingThread.Abort();    //abort threads
                    alarmThread.Abort();

                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                    }

                    serialPortTextBox.Enabled = true;
                    //End log and output file
                    String desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    File.WriteAllText(desktopPath + @"\Telemetry " + DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".csv", onlineCsv.ToString());
                    logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Telemetry ended correctly.");
                    logFile.WriteLine("**********************************************************************************");
                    logFile.Close();
                }
                catch (Exception ex)
                {
                    logFile.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + ": Failed to end telemetry. Error: " + ex.Message);
                    logFile.WriteLine("**********************************************************************************");
                }
            }
        }
        /* Event: FastForwardButton_Click
        * 
        * For OFFLINE only, the event will be called when the user press the Fast Forward button.
        * the event will decrease the presentation timer interval to present the data faster.
        */
        private void FastForwardButton_Click(object sender, EventArgs e)
        {
            if (refreshTimer.Interval > 5)
            {
                refreshTimer.Interval -= 5;
                UpdateTimeSpeedLabel();
            }
        }
        /* Event: ReverseButton_Click
        * 
        * For OFFLINE only, the event will be called when the user press the Reverse button.
        * the event will increase the presentation timer interval to present the data slower.
        */
        private void ReverseButton_Click(object sender, EventArgs e)
        {
            if (refreshTimer.Interval < 100)
            {

                refreshTimer.Interval += 5;
                UpdateTimeSpeedLabel();
            }
        }
        /* Event: onlineOptionButton_CheckedChanged
        *  
        * the event will update objects to prepare the program for online telemetry
        */
        private void onlineOptionButton_CheckedChanged(object sender, EventArgs e)
        {
            csvFilePanel.Visible = false;
            diagnosticsPanel.Visible = true;
            diagnosticsToolStripMenuItem.Enabled = true;
            onlineOptionPanel.Visible = true;
            setEnduranceButton.Visible = true;

            ReverseButton.Enabled = false;
            FastForwardButton.Enabled = false;
        }
        /* Event: csvOptionButton_CheckedChanged
        *  
        * the event will update objects to prepare the program for offline presentation
        */
        private void csvOptionButton_CheckedChanged(object sender, EventArgs e)
        {
            csvFilePanel.Visible = true;
            diagnosticsPanel.Visible = false;
            diagnosticsToolStripMenuItem.Enabled = false;
            onlineOptionPanel.Visible = false;

            ReverseButton.Enabled = true;
            FastForwardButton.Enabled = true;
        }
        /* Event: openCSVButton_Click
        * 
        * will be called when the user press the open csv file button 
        * the event will open file dialog to let the user choose csv file
        */
        private void openCSVButton_Click(object sender, EventArgs e)
        {
            openCSVDialog.Filter = "CSV|*.csv";
            if (openCSVDialog.ShowDialog() == DialogResult.OK)
            {
                csvPathLabel.Text = openCSVDialog.FileName;
                csvPath = openCSVDialog.FileName;
            }
        }
        /* Event: graphBmpPictureBox_Paint
        * 
        * this is a paint event, will be called when the command Invalidate will performed.
        * the event is in charge of updating the graph each iteration with the new data.
        */
        private void graphBmpPictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = Graphics.FromImage(graphBmp);

            if (timerCounter == 0) {
                Pen darkGrayPen = new Pen(Color.DimGray, 1);

                g.DrawLine(darkGrayPen, new Point(0, 3 * graphBmpPictureBox.Height / 4) , new Point(graphBmpPictureBox.Width,3 * graphBmpPictureBox.Height / 4));
                g.DrawLine(darkGrayPen, new Point(0, graphBmpPictureBox.Height / 2), new Point(graphBmpPictureBox.Width, graphBmpPictureBox.Height / 2));
                g.DrawLine(darkGrayPen, new Point(0, graphBmpPictureBox.Height / 4), new Point(graphBmpPictureBox.Width, graphBmpPictureBox.Height / 4));

                int minutesToShow, secondsToShow;
                String timeToPrint;

                for (int i=0;i< graphBmpPictureBox.Width; i+= 300) {
                   
                    minutesToShow = i / 1200;
                    secondsToShow = (i % 1200) / 20;
                    timeToPrint = "";

                    if (minutesToShow < 10)
                    {
                        timeToPrint = "0";
                    }
                    timeToPrint += minutesToShow.ToString() + ":";
                    if (secondsToShow < 10)
                    {
                        timeToPrint += "0";
                    }
                    timeToPrint += secondsToShow.ToString();

                    g.DrawString(timeToPrint, timeGraphLabel.Font, Brushes.White, i, 7 * graphBmpPictureBox.Height / 8);
                }
            }

            int graphScrollBarDeviation = 20;

            //Paint temperature
            Point oldTempPoint = new Point(timerCounter, graphBmpPictureBox.Height - graphScrollBarDeviation - (int)((oldTempValue - 50) * 4.33));
            Point newTempPoint = new Point(timerCounter + 1, graphBmpPictureBox.Height - graphScrollBarDeviation - (int)((newTempValue - 50) * 4.33));
            g.DrawLine(tempPen, oldTempPoint, newTempPoint);

            //Paint RPM
            Point oldRpmPoint = new Point(timerCounter, graphBmpPictureBox.Height - graphScrollBarDeviation - (oldRpmValue / 46));
            Point newRpmPoint = new Point(timerCounter + 1, graphBmpPictureBox.Height - graphScrollBarDeviation - (newRpmValue / 46));
            g.DrawLine(rpmPen, oldRpmPoint, newRpmPoint);

            g.Dispose();
        }
        /* Event: graphBmpPictureBox_MouseMove
        * 
        * this is a mouse move event, will be called when the software will detect mouse move over the graph.
        * the event is in charge of updating the graph's pointed data so the user will know exactly how much 
        * rpm/temp etc there is in the location he's pointing to.
        */
        private void graphBmpPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            //Print viewed data by mouse location - the deviation in the calculation is based on manual checks

            //time value
            int minutesToShow = (Cursor.Position.X - this.Location.X - graphBmpPictureBox.Location.X - 335) / 1200;
            int secondsToShow = ((Cursor.Position.X - this.Location.X - graphBmpPictureBox.Location.X - 335) % 1200) / 20;
            String timeToShow = "";

            if (minutesToShow < 10)
            {
                timeToShow = "0";
            }
            timeToShow += minutesToShow.ToString() + ":";
            if (secondsToShow < 10)
            {
                timeToShow += "0";
            }
            timeToShow += secondsToShow.ToString();

            timeGraphLabel.Text = "time: " + timeToShow;

            //Rpm value
            int rpmFromMouseMove = Convert.ToInt32(((graphPictureBox.Height - (Cursor.Position.Y - graphPictureBox.Location.Y - this.Location.Y + 12)) * 46) + 1150);
            if (rpmFromMouseMove < 0)
            {
                rpmGraphLabel.Text = "rpm"; //fix if the mouse is over the scrollbar
            }
            else
            {
                rpmGraphLabel.Text = "rpm: " + rpmFromMouseMove.ToString();
            }

            //Temperature value
            int tempFromMouseMove = Convert.ToInt32(((graphPictureBox.Height - (Cursor.Position.Y - graphPictureBox.Location.Y - this.Location.Y + 12)) / 4.3) + 56);
            if (tempFromMouseMove < 50)
            {
                tempGraphLabel.Text = "temperature"; //fix if the mouse is over the scrollbar
            }
            else
            {
                tempGraphLabel.Text = "temperature: " + tempFromMouseMove.ToString();
            }
        }
        /* Event: graphBmpPictureBox_MouseLeave
        * 
        * this is a mouse leave event, will be called when the software will detect that the mouse left the 
        * graph area.
        * the event is in charge of clean the graph's pointed data labels.
        */
        private void graphBmpPictureBox_MouseLeave(object sender, EventArgs e)
        {
            //Clean labels after the mouse leaves the area
            timeGraphLabel.Text = "time";
            tempGraphLabel.Text = "temperature";
            rpmGraphLabel.Text = "rpm";
        }
        /* Event: TempGraphScrollBar_Scroll
        * 
        * this is a scroll event, will be called when the user touch the scroll bar.
        * the event is in charge of updating the graph location according to the scroll bar state.
        */
        private void TempGraphScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            scrollBarInUse = true;
            graphBmpPictureBox.Location = new Point(graphPictureBox.Location.X - (graphPictureBox.Width/2) - graphScrollBar.Value, graphBmpPictureBox.Location.Y);

            if (graphScrollBar.Value > (graphScrollBar.Maximum - graphScrollBar.Maximum/10)) {

                graphScrollBar.Value = graphScrollBar.Maximum;
                scrollBarInUse = false;
            }
        }
        /* Event: setEnduranceButton_Click
        * 
        * prepare visual objects to let the user configure endurance requirements.
        */
        private void setEnduranceButton_Click(object sender, EventArgs e)
        {
            enduranceTimePanel.Visible = true;
            setEnduranceButton.Visible = false;
            setFuelCapacityButton.Visible = false;

            startButton.Enabled = false;
            stopButton.Enabled = false;
        }
        /* Event: enduranceSetButton_Click
        * 
        * return visual objects to their state after user finished configure endurance.
        */
        private void enduranceSetButton_Click(object sender, EventArgs e)
        {
            enduranceTimePanel.Visible = false;
            setEnduranceButton.Visible = true;
            setFuelCapacityButton.Visible = true;

            startButton.Enabled = true;
            stopButton.Enabled = true;
        }
        /* Event: enduranceTimeTrackBar_Scroll
        * 
        * this is a track bar event, will be called when the user touch the track bar.
        * the event is in charge of updating the endurance length according to the track bar state.
        */
        private void enduranceTimeTrackBar_Scroll(object sender, EventArgs e)
        {
            enduranceLengthInSeconds = enduranceTimeTrackBar.Value;

            //update time label
            enduranceTimeLabel.Text = "";
            int timeMinutes = enduranceLengthInSeconds / 60;
            int timeSeconds = enduranceLengthInSeconds % 60;
            if (timeMinutes < 10)
            {
                enduranceTimeLabel.Text = "0";
            }
            enduranceTimeLabel.Text += timeMinutes.ToString() + ":";
            if (timeSeconds < 10)
            {
                enduranceTimeLabel.Text += "0";
            }
            enduranceTimeLabel.Text += timeSeconds.ToString();
        }
        /* Event: setFuelCapacityButton_Click
        * 
        * prepare visual objects to let the user configure fuel capacity.
        */
        private void setFuelCapacityButton_Click(object sender, EventArgs e)
        {
            fuelCapacitySetPanel.Visible = true;
            setEnduranceButton.Visible = false;
            setFuelCapacityButton.Visible = false;

            startButton.Enabled = false;
            stopButton.Enabled = false;
        }
        /* Event: fuelCapacitySetButton_Click
        * 
        * return visual objects to their state after user finished configure fuel capacity.
        */
        private void fuelCapacitySetButton_Click(object sender, EventArgs e)
        {
            fuelCapacitySetPanel.Visible = false;
            setEnduranceButton.Visible = true;
            setFuelCapacityButton.Visible = true;

            startButton.Enabled = true;
            stopButton.Enabled = true;
        }
        /* Event: fuelCapacityTrackBar_Scroll
        * 
        * this is a track bar event, will be called when the user touch the track bar.
        * the event is in charge of updating the fuel capacity according to the track bar state.
        */
        private void fuelCapacityTrackBar_Scroll(object sender, EventArgs e)
        {
            //Update fuel capacity
            fuelCapacity = fuelCapacityTrackBar.Value / 100.0;

            fuelCapacityLabel.Text = fuelCapacity.ToString();
            if (fuelCapacityLabel.Text.Contains(".")) {

                fuelCapacityLabel.Text = (fuelCapacityLabel.Text + "00").Substring(0, 4) + " L";
            } else {

                fuelCapacityLabel.Text = (fuelCapacityLabel.Text + ".00").Substring(0, 4) + " L";
            }

            if (fuelCapacityPictureBox.Height >= 0) { //update fuel bar

                fuelCapacityPictureBox.Height = Convert.ToInt32((fuelCapacity / 5) * fuelBarHeight);
                fuelCapacityPictureBox.Location = new Point(fuelCapacityPictureBox.Location.X, fuelBarHeightLocation + (fuelBarHeight - Convert.ToInt32((fuelCapacity / 5) * fuelBarHeight)));
            }

            if (fuelCapacityPictureBox.Height >= 160) {//update the fuel bar color

                fuelCapacityPictureBox.BackColor = Color.Lime;
            } else if (fuelCapacityPictureBox.Height < 160 && fuelCapacityPictureBox.Height >= 120) {

                fuelCapacityPictureBox.BackColor = Color.GreenYellow;
            } else if (fuelCapacityPictureBox.Height < 120 && fuelCapacityPictureBox.Height >= 80) {

                fuelCapacityPictureBox.BackColor = Color.Yellow;
            } else if (fuelCapacityPictureBox.Height < 80 && fuelCapacityPictureBox.Height >= 40) {

                fuelCapacityPictureBox.BackColor = Color.Orange;
            } else {

                fuelCapacityPictureBox.BackColor = Color.Red;
            }
        }
        /* Event: aboutToolStripMenuItem_Click
        * 
        * when pressed, open about form.
        */
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog(this);
        }
        /* Event: exitToolStripMenuItem_Click
        * 
        * when pressed, close program.
        */
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /* Event: diagnosticsToolStripMenuItem_Click
        * 
        * when pressed, show/hide diagnostics section.
        */
        private void diagnosticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (diagnosticsPanel.Visible == false)
            {
                diagnosticsPanel.Visible = true;
            } else {
                diagnosticsPanel.Visible = false;
            }
        }
        /* Event: MainForm_FormClosing
        * 
        * this event will be called when the user will close the form. 
        * the event will take care of ending the online telemetry in case the user forgot.
        */
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (stopButton.Enabled)
            {
                stopButton.PerformClick();
            }

            Application.Exit();
        }

        /*************************************************************************************************** 
         *                                      Utilities Functions                                        *
         ***************************************************************************************************/
        /* Function: RotateImg
        * 
        * the function gets an image, an angle and a size.
        * this function will serve the analog clocks by rotating their indicators according to the requested angle.
        * the function will rotate the image by it center, according to the given angle.
        * the function will return the rotated image.
        */
        public Image RotateImg(Image bmp, float angle, int bmpSize)
        {
            bmp = new Bitmap(bmp, (bmp.Width * bmpSize) / 100, (bmp.Height * bmpSize) / 100);

            if (angle < MINIMAL_DEGREE)
            {
                angle = MINIMAL_DEGREE;
            }
            else if (angle > MAXIMAL_DEGREE)
            {
                angle = MAXIMAL_DEGREE;
            }

            angle = angle % 360;
            if (angle > 180)
                angle -= 360;

            System.Drawing.Imaging.PixelFormat pf = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

            float sin = (float)Math.Abs(Math.Sin(angle * Math.PI / 180.0)); // this function takes radians
            float cos = (float)Math.Abs(Math.Cos(angle * Math.PI / 180.0)); // this one too

            float newImgWidth = sin * bmp.Height + cos * bmp.Width;
            float newImgHeight = sin * bmp.Width + cos * bmp.Height;

            float originX = 0f;
            float originY = 0f;

            if (angle > 0)
            {
                if (angle <= 90)
                    originX = sin * bmp.Height;
                else
                {
                    originX = newImgWidth;
                    originY = newImgHeight - sin * bmp.Width;
                }
            }
            else
            {
                if (angle >= -90)
                    originY = sin * bmp.Width;
                else
                {
                    originX = newImgWidth - sin * bmp.Height;
                    originY = newImgHeight;
                }
            }

            Bitmap newImg = new Bitmap((int)newImgWidth, (int)newImgHeight, pf);

            Graphics g = Graphics.FromImage(newImg);
            g.Clear(Color.Transparent);

            g.TranslateTransform(originX, originY); // offset the origin to our calculated values
            g.RotateTransform(angle); // set up rotate
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.DrawImageUnscaled(bmp, 0, 0); // draw the image at 0, 0
            g.Dispose();

            return newImg;
        }
        /* Function: convertMessageHexToNumber
         * 
         * the function gets a hexadecimal string presented as [LowByte,HighByte] and a conversion rate.
         * the function will convert the hexadecimal to decimal and will return the calculated value 
         * (LowByte+(HighByte*256)*conversionRate).
         */
        private double convertMessageHexToNumber(string hexString, double conversionRate) {

            if (hexString.Length == 4)
            {
                return ((int.Parse(hexString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) + (int.Parse(hexString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber)*256))*conversionRate);
            }

            return 0;
        }
        /* Function: convertGearVoltagetoLevel
         * 
         * the function gets the gear voltage that has been sent from the ECU.
         * the function will replace the voltage with the real gear level as configured by the gear changing 
         * engine.
         */
        private string convertGearVoltagetoLevel(double gearVoltage)
        {
            if (gearVoltage > 0.5 && gearVoltage < 0.8) {

                return "1";
            } else if (gearVoltage > 1.25 && gearVoltage < 1.8) {

                return "2";
            } else if (gearVoltage > 1.85 && gearVoltage < 2.5) {

                return "3";
            } else if (gearVoltage > 2.6 && gearVoltage < 3.3) {

                return "4";
            } else {

                return "N";
            }
        }
        /* Function: UpdateTimeSpeedLabel
        * 
        * the function will use the refresh timer interval rate to calculate and display the rate of the 
        * offline presentation for the user.
        * for example: x1.00 is normal speed. x2.00 is twice as fast from the regular speed.
        */
        private void UpdateTimeSpeedLabel()
        {
            String TimeSpeedString = ((float)50 / refreshTimer.Interval).ToString();

            if (!TimeSpeedString.Contains("."))
            {
                TimeSpeedString += ".";
            }
            TimeSpeedString += "00";

            if (TimeSpeedString.Length > 4)
            {
                TimeSpeedString = TimeSpeedString.Substring(0, 4);
            }
            TimeSpeedLabel.Text = "x" + TimeSpeedString;
        }
    }
}