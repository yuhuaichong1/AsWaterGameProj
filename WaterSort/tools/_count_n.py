import json
rows=json.load(open(r"d:\UnityProj\_Project\AsWaterGameProj\WaterSort\Assets\WaterGame\Editor\LevelEditor\LevelDesignV1.json",encoding="utf-8"))
for r in rows:
    n=r["regular"]+r["empty"]+r["lockCup"]+r["ad"]
    if r["level"] in [1,5,12,36,42,60]:
        print(f"L{r['level']:02d} N={n}")
print("max", max(r["regular"]+r["empty"]+r["lockCup"]+r["ad"] for r in rows))
