# NW-HideByCoords-Faster (proof-of-concept, sample code, use at own risk)
Similar to "NW-HideByCoords", but faster by using Clasher. Requires some setup in the NW (NW = Navisworks) file though. 

How to install and use from batch file, see here: https://github.com/Henaccount/NW-HideByCoords
(addition in this script: using the origin for the min and max points will lead to the "Unhide All" behaviour)
Using from batch file can be interesting in the context of "splitting" the NW model into "pieces", meaning NW files saved with different areas visible, for better performance on BIM 360.

Additionally you can use it in Navisworks (tested with NW Manage 2020). You select a tree element and execute the script. As a result you will just get the area shown that is covered or touched by the bounding box of the tree element. Also the file will be saved with this visibility state. This can be interesting in the context of attaching the NW model to an ACAD drawing as a "Coordination Model" (by xref GUI). Like this only parts of the NW model will be shown in ACAD for coordination of additional ACAD objects (e.g. pipes).

For the same purpose as above paragraph, but for avoiding to access the Navisworks application during working with ACAD, you can use in ACAD the following script: https://github.com/Henaccount/ClipBox-ACAD-NW which will create and execute a batch file in the documents folder which calls this NW script silently (headless).


# Preparation of the Navisworks file for use with this script:
Note that this has just been tested with a metric Navisworks model (units = meter) together with Plant 3D (units = milimeter). For different unit situations code should take care, but not tested.
<li>Unhide All in your Navisworks model
<li>Append to your Navisworks file the "CoordinationClipBox.dwg" which can be downloaded from the root of this repository.
<li>Create a selection set from the (only) 3D Solid that is contained in the CoordinationClipBox.dwg and name it "clipbox"
<li>Create a clash check like this and move it to the very top of the list (if several exist), the rest of the settings will be added automatically:
  
  <img src="https://github.com/Henaccount/NW-HideByCoords-Faster/blob/master/clasher-settings.png?raw=true">
  
  <li>Save the file in your Documents folder and call it: "CoordinationModel.nwd"


