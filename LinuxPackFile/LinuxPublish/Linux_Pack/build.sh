#入参 1为arm64或者amd64 2为版本号
#第一次运行之前需要对此脚本文件进行权限添加，命令为 chmod +x build.sh
#需要将此脚本与dll目录与amd64或者arm64放置与同一目录
#后续运行只需要 ./build amd64 1.1.1.1即可
if [ $# != 2 ]
then
    echo "请正确传入参数:参数1为arm64或者amd64,参数2为版本号"
    exit
fi
sed -i "s/\(Version: \).*/\1$2/" $1/DEBIAN/control
sed -i "s/\(\"version\": \).*/\1\"$2\",/" $1/opt/apps/cn.com.10jqka/info
rootfile="linux-arm64"
ceffile="cef_linux_arm64.7z"
if [ $1 == arm64 ]
then 
    rootfile="linux-arm64"
    ceffile="cef_linux_arm64.7z"
elif [ $1 == amd64 ]
then
    rootfile="linux-x64"
    ceffile="cef_linux_amd64.7z"
fi
for file in $1/opt/apps/cn.com.10jqka/files/*
do
    if [ -f "$file" ]
    then
        rm -fv $file
    fi
done
cp -rf ../$rootfile/. $1/opt/apps/cn.com.10jqka/files
7z x ../$ceffile -o$1/opt/apps/cn.com.10jqka/files

chmod -R 755 $1/DEBIAN
chmod -R 777 $1/opt/apps
chmod -R 755 $1/opt/apps/cn.com.10jqka/info
dpkg-deb --root-owner-group -b $1 cn.com.10jqka_$2_$1.deb