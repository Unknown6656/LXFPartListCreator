function tprint([String] $text) {
	echo "-----------------------------------------------"
	echo "       $text ..."
	echo "-----------------------------------------------"
}

tprint "GENERATING RAW PART LIST"
..\LXFPartListCreator.exe --in=./test.lxf --type=raw
tprint "GENERATING HTML FILE"
..\LXFPartListCreator.exe --in=./test.raw --raw --type=html
tprint "GENERATING JSON FILE"
..\LXFPartListCreator.exe --in=./test.raw --raw --type=json
tprint "GENERATING XML FILE"
..\LXFPartListCreator.exe --in=./test.raw --raw --type=xml
tprint "GENERATING CSV FILE"
..\LXFPartListCreator.exe --in=./test.raw --raw --type=excel --x-type=csv
tprint "GENERATING XLS FILE"
..\LXFPartListCreator.exe --in=./test.raw --raw --type=excel --x-type=xls
tprint "GENERATING XLSX FILE"
..\LXFPartListCreator.exe --in=./test.raw --raw --type=excel --x-type=xlsx
