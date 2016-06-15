# CSharpRemoteSupport
C# Client and Server for remote support

An attempt to make a remote support application.  It is similar to VNC but is not compatible with it.  
*Support File Transfers (in progress)
*Multiple monitor support
*Need to add clipboard transfer support.  Do not need it to be automatic.

The client UI for now is just a start button and a log window.  It can be modified to add multiple locations to connect to.
Screen capture process
-Grab full screen shot and store it
-Send Full Screen shot as JPG or PNG.  Currently I have it doing both and sending whichever is smaller.  It eats up CPU but saves bandwidth.  It can be turned off.  PNG offers better text clarity while JPG is better for graphics and when looking at a web browser
-Wait for server ACK 
-Iteriate through the screen to look for changes
--Take a new screen shot and compare to last
---First optimization was to compare line by line.  If the line is eactly the same (memcmp) then go to the next.  If the line is different we need to mark that square as different
---Iterate through the squares map and send each square.  It compresses each via JPG and PNG and sends the smaller and this can be changed
---Once done send message that squares are done
---Go back and do it again

