Unity API. It allows to send command from Android to Uinity

Notes:
All agrguments are strings as Android passes strings as parameters


void LoadModel(string uri);
Loads model by specified uri. This uri should be local file

void AppendModel(string url);
Appends model by specified uri. Similar to LoadModels but doesn't remove old model.
Can be used for different clothers

SelectCamera(string intStr);
Allows to change camera. Should be integer string "0", "1"



ShowUI(string boolStr)
Debug API: Shows Unity debugging UI 

ShowHelpers(string boolStr);
DebugAPI: Enables or disables landmarks points and skeleton. Should be a boolean string

void SetSkinScaleX(string floatValue);
void SetSkinScaleZ(string floatValue);
DebugAPI: Sets a scales for skin. It allows to make model large or smaller (fitness feature)

ShowFaceMask(string boolStr);
Debug API: Makes face mask visible

		
	
Android usage:
API command are implemented in UnityMessage class

Example
object EnableBody : UnityMessage(methodName = "ShowHelpers", args="true")
