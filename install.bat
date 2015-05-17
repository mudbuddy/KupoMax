taskkill /f /im RebornBuddy.exe
pushd e:\rb\Routines
rm -rf Kupo
mkdir Kupo
pushd C:\Users\mudzereli\Documents\GitHub\KupoMax
xcopy /y /s KupoMax e:\rb\Routines\Kupo
popd
cd ..
start RebornBuddy.exe