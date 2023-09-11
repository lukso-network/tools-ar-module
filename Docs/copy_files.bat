set dst=..\digitalwardrobe-android-app\digitalwardrobe-ar-module-prebuilt\


rm -r %dst%\unityLibrary\libs
rm -r %dst%\unityLibrary\symbols
rm -r %dst%\unityLibrary\src\main\assets
rm -r %dst%\unityLibrary\src\main\Il2CppOutputProject
rm -r %dst%\unityLibrary\src\main\jniLibs
rm -r %dst%\unityLibrary\src\main\jniStaticLibs


xcopy /E /I /Y .\unitylibrary\libs %dst%\unityLibrary\libs
xcopy /E /I /Y .\unitylibrary\symbols %dst%\unityLibrary\symbols
xcopy /E /I /Y .\unitylibrary\src\main\assets %dst%\unityLibrary\src\main\assets
xcopy /E /I /Y .\unitylibrary\src\main\Il2CppOutputProject %dst%\unityLibrary\src\main\Il2CppOutputProject
xcopy /E /I /Y .\unitylibrary\src\main\jniLibs %dst%\unityLibrary\src\main\jniLibs
xcopy /E /I /Y .\unitylibrary\src\main\jniStaticLibs %dst%\unityLibrary\src\main\jniStaticLibs