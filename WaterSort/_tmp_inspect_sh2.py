import json
p = r"d:\BaiduNetdiskDownload\水排序\WaterSort\Assets\WaterGame\Resources\Spine\shui_hua\sh.json"
d = json.load(open(p, encoding="utf-8"))
anim = d["animations"]["shuihua"]
for bone in ["bone","bone2","bone3","bone4"]:
    sc = anim["bones"][bone]["scale"]
    print(bone, sc)
