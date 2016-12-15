using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace FootfallWithCamera
{
    public partial class Form1 : Form
    {
        // These objects are all to allow us to use the Web Cam
        // Don't change them
        private AForge.Video.DirectShow.FilterInfoCollection videoDevices;
        private AForge.Video.DirectShow.VideoCaptureDevice videoDevice;
        private AForge.Video.DirectShow.VideoCapabilities[] videoCapabilities;
        private AForge.Video.DirectShow.VideoCapabilities[] snapshotCapabilities;

        // These are our Microsoft API Keys, replace them with yours
        private const String MS_COG_SERVICE = "3e91eaa9d6fe458aaef7274ef14d2345";
        private const String MS_COG_SERVICE_2 = "5bd6eb19dfde40caa6e8d9beb7092b17";

        // This is what UA Number you want your hit sent to
        private const String Google_UA_Number = "UA-4532898-49";

        public Form1()
        {
            InitializeComponent();
        }

        // Click handler to detect Web Cams on the machine
        private void button1_Click(object sendersnd, EventArgs eargs)
        {
            // Clear the list of Web Cams
            comboBox1.Items.Clear();

            // Use the API to get a list of Web Cams
            videoDevices = new AForge.Video.DirectShow.FilterInfoCollection(AForge.Video.DirectShow.FilterCategory.VideoInputDevice);

            // For each Web Cam add it to the list
            foreach (AForge.Video.DirectShow.FilterInfo device in videoDevices)
            {
                comboBox1.Items.Add(device.Name);
               
            }

        }

        // When we pick a Web Cam set our Video Device to the Web Cam Selected
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Make sure we have something selected
            if (videoDevices.Count != 0)
            {
                // Use the API to get the capabilities of that Web Cam
                videoDevice = new AForge.Video.DirectShow.VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
                EnumeratedSupportedFrameSizes(videoDevice);
            }
        }

        // This gets the capabilities of the WEb Cam
        private void EnumeratedSupportedFrameSizes(AForge.Video.DirectShow.VideoCaptureDevice videoDevice)
        {
            this.Cursor = Cursors.WaitCursor;

            // We clear our capabilities combo boxes
            videoResolutionsCombo.Items.Clear();
            snapshotResolutionsCombo.Items.Clear();

            try
            {
                // We then read the attributes from the device selected
                videoCapabilities = videoDevice.VideoCapabilities;
                snapshotCapabilities = videoDevice.SnapshotCapabilities;

                // We then iterate through its capaibilities (Camera Resolution) adding these to the first Combo Box
                foreach (AForge.Video.DirectShow.VideoCapabilities capabilty in videoCapabilities)
                {
                    videoResolutionsCombo.Items.Add(string.Format("{0} x {1}",
                        capabilty.FrameSize.Width, capabilty.FrameSize.Height));
                }

                // We then iterate through its capaibilities (Snaphot Resolution) adding these to the first Combo Box
                foreach (AForge.Video.DirectShow.VideoCapabilities capabilty in snapshotCapabilities)
                {
                    snapshotResolutionsCombo.Items.Add(string.Format("{0} x {1}",
                        capabilty.FrameSize.Width, capabilty.FrameSize.Height));
                }

                // If we didn't find anything add a not supported to the list
                if (videoCapabilities.Length == 0)
                {
                    videoResolutionsCombo.Items.Add("Not supported");
                }
                if (snapshotCapabilities.Length == 0)
                {
                    snapshotResolutionsCombo.Items.Add("Not supported");
                }

                // Set the index (default) to 0
                videoResolutionsCombo.SelectedIndex = 0;
                snapshotResolutionsCombo.SelectedIndex = 0;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

       
        private void connectButton_Click_1(object sender, EventArgs e)
        {
            
            // Make sure we have a selected video device
            if (videoDevice != null)
            {
                // We need it to have capabilities
                if ((videoCapabilities != null) && (videoCapabilities.Length != 0))
                {
                    videoDevice.VideoResolution = videoCapabilities[videoResolutionsCombo.SelectedIndex];
                }

                // We can't do snapshots if it doesn't support them!
                if ((snapshotCapabilities != null) && (snapshotCapabilities.Length != 0))
                {
                    videoDevice.ProvideSnapshots = true;
                    videoDevice.SnapshotResolution = snapshotCapabilities[snapshotResolutionsCombo.SelectedIndex];
                    videoDevice.SnapshotFrame += new AForge.Video.NewFrameEventHandler(videoDevice_SnapshotFrame);
                }

                
                // Once it's enabled we want to disable things that change what we are capturing from
                EnableConnectionControls(false);

                // Start capturing
                videoSourcePlayer.VideoSource = videoDevice;
                videoSourcePlayer.Start();
            }
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {
            // This essentially will disconnect us from capturing
            Disconnect();
        }

        private void Disconnect()
        {
            // Make sure we have a video source
            if (videoSourcePlayer.VideoSource != null)
            {
                // Stop it capturing
                videoSourcePlayer.SignalToStop();
                videoSourcePlayer.WaitForStop();
                videoSourcePlayer.VideoSource = null;
                
                if (videoDevice.ProvideSnapshots)
                {
                    videoDevice.SnapshotFrame -= new AForge.Video.NewFrameEventHandler(videoDevice_SnapshotFrame);
                }

                // Reneable the controls we turned off
                EnableConnectionControls(true);
            }
        }

        private void EnableConnectionControls(bool enable)
        {
            // This enables controls
            videoResolutionsCombo.Enabled = enable;
            snapshotResolutionsCombo.Enabled = enable;
            connectButton.Enabled = enable;
            disconnectButton.Enabled = !enable;
           
        }

        // We don't actually use this code as when
        // I modified the AForge code my WebCam didn't support snapshots in this way!
        private void videoDevice_SnapshotFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            // When this snapshot event happens, clone the current frame to a Bitmap and save it 

            Bitmap objBmp;

            objBmp = (Bitmap)eventArgs.Frame.Clone();

            objBmp.Save("c:\\temp\\test-" + DateTime.Now.ToString("yyyymmddhhmmss") + ".bmp");

        }

        // If you click the capture button
        private void btnCapture_Click(object sender, EventArgs e)
        {
            // A bitmap object
            Bitmap objBmp;
             Rectangle r;

            // Create a rectangle, this matches the size of my snapsot
            r = new Rectangle(0, 0, 800, 465);

            // Create a picture
            objBmp = new Bitmap(800, 465);
            
            // Draw the current frame to a Bitmap
            videoSourcePlayer.DrawToBitmap(objBmp, r);

            // And save it in the temp folder timestamped
            objBmp.Save("c:\\temp\\test-" + DateTime.Now.ToString("yyyymmddhhmmss") + ".bmp");

            

        }

        private void CountFaces(String strImage)
        {
            
            String strURL;
            
            // Variables to store data coming back from the Microsoft Cognitive API
            String strJSON;
            String strAge;
            String strSex;

            // Some objects we use when sending hits to GA

            System.Net.HttpWebRequest webreq;   // Request
            System.Net.WebResponse webresp;     // Response
            System.IO.Stream stream3;           // Stream to read response
            System.IO.StreamReader reader3;     // Stream reader to read the stream into a string

            // Variables to hold the Measurement Protocol Request and the Return
            String strGoogleAnalytics;
            String strReturn;

            // Depending upon how you want to do things, you can use one Session
            // Or we could generate a client id each time we capture, I am using one Client Id
            String Google_Client_Id = "12345.12345";

            // How many faces are in the picture
            int NumberOfFaces;

            // Initialise all to nothing
            strJSON = "";
            strAge = "";
            strSex = "";

            // This is the Microsoft API Endpoint
            strURL = "https://api.projectoxford.ai/vision/v1.0/analyze?visualFeatures=Faces&language=en";

            // It's a little messy uploading an image as a multipart/form but this does it
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            // We have a collection, this won't be used as we aren't passing any form fields
            System.Collections.Specialized.NameValueCollection nvc;
            nvc = new System.Collections.Specialized.NameValueCollection();

            // Create our request, note the head Ocp-Apim-Subscription-Key
            // This is our Microsoft API key
            System.Net.HttpWebRequest wr = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(strURL);
            wr.Headers.Add("Ocp-Apim-Subscription-Key", MS_COG_SERVICE_2);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;

            // Grab a stream, this allows us to push data into the request
            System.IO.Stream rs = wr.GetRequestStream();

            // This sets the template for form fields
            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";

            // Append any fields we may want to push with the request
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }

            // Write our form fields to the request
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            // Now we handle the file upload itself
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, "filImage", strImage, "image/bmp");
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            // Read the file and write it to the request, the File Stream is us reading it from disk
            // rs is us writing it to the Web Request
            System.IO.FileStream fileStream = new System.IO.FileStream(strImage, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }

            // After we've read the image close it
            fileStream.Close();

            // Close the request
            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            // Try and get the response
            System.Net.WebResponse wresp = null;
            try
            {
                // We get the response and create a stream reader to allow us
                // To read the response into a String (called strJSON)
                wresp = wr.GetResponse();
                System.IO.Stream stream2 = wresp.GetResponseStream();
                System.IO.StreamReader reader2 = new System.IO.StreamReader(stream2);
                strJSON = reader2.ReadToEnd();
                Debug.WriteLine(string.Format("File uploaded, server response is: {0}", strJSON));
            }
            catch (Exception ex)
            {
                // If it fails we bail!
                Debug.WriteLine("Error uploading file", ex);
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }

            // Assuming the request is successful our JSON will be populated
            if (strJSON != "")
            {
                // These objects allow us to manipulate JSON in C#.NET
                Newtonsoft.Json.Linq.JObject obj;
                IList<Newtonsoft.Json.Linq.JToken> results;
                Newtonsoft.Json.Linq.JToken jtFace;

                System.Collections.Generic.IEnumerator<Newtonsoft.Json.Linq.JToken> enm;

                // We try and parse the JSON into a JObject
                obj = Newtonsoft.Json.Linq.JObject.Parse(strJSON);

                // And set our face count to 0
                NumberOfFaces = 0;

                // We then select the faces returned element in the JSON and convert it
                // To as C#.NET list - this allows us to enumerate it
                // As well as count the number of items (faces)
                try
                {
                    // Convert this to a list
                    results = (IList<Newtonsoft.Json.Linq.JToken>)obj["faces"].Children().ToList();

                    // Count the faces
                    NumberOfFaces = results.Count;

                    // Only send if we have faces
                    if (NumberOfFaces > 0)
                    { 
                    
                        // We build our Measurement Protocol Hit Payload
                        // This sends the capture as a Pageview, you can change this to anything
                        // This should be made into a function but for now its repeated to make it easier
                        // to follow
                        strGoogleAnalytics = "https://www.google-analytics.com/collect?";
                        strGoogleAnalytics += "v=1&";
                        strGoogleAnalytics += "t=pageview&";
                        strGoogleAnalytics += "tid=" + Google_UA_Number + "&";
                        strGoogleAnalytics += "cid=" + Google_Client_Id + "&";
                       
                        // We set our page level attributes
                        strGoogleAnalytics += "dp=%2Fface%2Fcapture";
                        strGoogleAnalytics += "&dt=FaceCapture%20" + NumberOfFaces.ToString() + "%20faces";

                        // And we increment Custom Metric 1 (Number of Faces)
                        strGoogleAnalytics += "&cm1=" + NumberOfFaces.ToString();

                        // We then use a Web Request to send this to the Google Servers
                        webreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(strGoogleAnalytics);
                        webresp = null;

                        // We are using a GET requests
                        webreq.Method = "GET";
                        webreq.KeepAlive = true;

                        // We create the response, but do nothing with it (other than write it to debug!)
                        webresp = webreq.GetResponse();
                        stream3 = wresp.GetResponseStream();
                        reader3 = new System.IO.StreamReader(stream3);
                        strReturn = reader3.ReadToEnd();

                        Debug.WriteLine(strReturn);

                        // We want to get the sex and age of each face as these are going 
                        // to be different events, so we use an enumarator
                        enm = results.GetEnumerator();

                        // This allows us to iterate faces!
                        // And send an event for each face
                        // Category : face capture
                        // Action : Male / Female
                        // Label : Age
                        // With custom dimensions and metrics

                        while (enm.MoveNext())
                        {
                            // Get our current face
                            jtFace = enm.Current;

                            try
                            {
                                // Read the gender attribute
                                strSex = jtFace["gender"].ToString();

                                // Read the age attribute
                                strAge = jtFace["age"].ToString();

                                // We build our Measurement Protocol Hit Payload
                                // This sends an event for
                                strGoogleAnalytics = "https://www.google-analytics.com/collect?";
                                strGoogleAnalytics += "v=1&";
                                strGoogleAnalytics += "t=event&";
                                strGoogleAnalytics += "tid=" + Google_UA_Number + "&";
                                strGoogleAnalytics += "cid=" + Google_Client_Id + "&";
                            
                                // Set our event attributes
                                strGoogleAnalytics += "ec=%2Fface%2Fcapture";
                                strGoogleAnalytics += "&ea=" + strSex;
                                strGoogleAnalytics += "&el=" + strAge;
                            
                                // Set Custom Dimension 1 to Age
                                strGoogleAnalytics += "&cd1=" + strAge;

                                // Set Custom Dimension 2 to Sex
                                strGoogleAnalytics += "&cd2=" + strSex;

                                // Set Custom Dimension 3 to the Date
                                strGoogleAnalytics += "&cd3=" + DateTime.Now.ToString("yyyy-mm-dd");

                                // Set Custom Dimension 4 to the Time
                                strGoogleAnalytics += "&cd4=" + DateTime.Now.ToString("HH:mm:ss");
                            
                                // If it's Female we increment Custom Metric 2, otherwise we increment Custom Metric 3
                                if (strSex == "Female")
                                {
                                    strGoogleAnalytics += "&cm2=1";
                                }
                                else
                                {
                                    strGoogleAnalytics += "&cm3=1";
                                }

                                // We then use a Web Request to send this to the Google Servers
                                webreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(strGoogleAnalytics);
                                webresp = null;

                                // We are using a GET requests
                                webreq.Method = "GET";
                                webreq.KeepAlive = true;

                                // We create the response, but do nothing with it (other than write it to debug!)
                                webresp = webreq.GetResponse();
                                stream3 = wresp.GetResponseStream();
                                reader3 = new System.IO.StreamReader(stream3);
                                strReturn = reader3.ReadToEnd();

                                Debug.WriteLine(strReturn);

                            
                            }
                            catch (Exception eFace)
                            {
                                // Uh oh, something wasn't right!
                                Debug.WriteLine(eFace.ToString());
                            }
                        }
                }

                    
                   
                    
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("Could not find any faces");
                }
                strJSON = "";
            }
            
        }

        // This enables the timer
        // The code in the timer captures an image every five seconds
        private void button2_Click(object sender, EventArgs e)
        {
            
            // If we are not enabled then enable the timer and set
            // the label
            if (label1.Text == "Disabled")
            {
                label1.Text = "Enabled";
                timer1.Interval = 5000;
                timer1.Enabled = true;
            }
            else
            {
                // Otherwise turn the timer off
                label1.Text = "Disabled";
                timer1.Enabled = false;
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Define a picture object and file path
            Bitmap objBmp;
            String strFile;
            Rectangle r;

            // The file is stored in the temporary folder with a timestamp
            strFile = "c:\\temp\\test-" + DateTime.Now.ToString("yyyymmddhhmmss") + ".bmp";

            // Define the image size
            r = new Rectangle(0, 0, 800, 465);
            objBmp = new Bitmap(800, 465);
            
            // Capture and save the current picture
            videoSourcePlayer.DrawToBitmap(objBmp, r);
            objBmp.Save(strFile);
            
            // Count the number of faces 
            CountFaces(strFile);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // This runs the count faces for a specfic named image
            CountFaces("c:\\temp\\test-20162315102355.bmp");

        }
        
    }
}
