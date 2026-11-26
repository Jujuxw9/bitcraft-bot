"""Tests du script de données (règles de D-077).

    py -3 -B -m unittest discover -s website-recipes/tests -p "test_*.py"

Tables inventées pour les règles précises ; tables réelles (build/raw) pour les cas de Julien, si présentes.
"""
import json
import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "tools"))
import recipes_core as core  # noqa: E402


def read_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def item(i, name, tier=1, tag="Thing", volume=600, rarity="Common", icon="", item_list_id=0):
    return {"id": i, "name": name, "tier": tier, "tag": tag, "volume": volume, "rarity": rarity,
            "icon_asset_name": icon, "description": "", "item_list_id": item_list_id}


def recipe(i, name, ins, outs, building=(25, 1)):
    return {"id": i, "name": name,
            "consumed_item_stacks": [{"item_id": a, "quantity": q, "item_type": t} for a, q, t in ins],
            "crafted_item_stacks": [{"item_id": a, "quantity": q, "item_type": t} for a, q, t in outs],
            "building_requirement": {"building_type": building[0], "tier": building[1]},
            "level_requirements": [{"skill_id": 6, "level": 1}], "tool_requirements": [], "actions_required": 10}


def base_tables():
    return {
        "item_desc": [
            item(1, "Ore Piece"), item(2, "Ingot"), item(3, "Axe", tier=2, volume=0), item(4, "Axe", tier=2, volume=0, rarity="Rare"),
            item(5, "Scrap"), item(6, "Ingot Output", item_list_id=90), item(7, "Pirate Weapon"), item(8, "Flux", tag="Trade Good"),
            item(9, "Ingot Package"),
        ],
        "cargo_desc": [item(100, "Ore Chunk", volume=6000), item(101, "Trunk", volume=6000), item(102, "Supply Crate", volume=6000)],
        "item_list_desc": [{"id": 90, "possibilities": [
            {"probability": 0.95, "items": [{"item_id": 2, "quantity": 1, "item_type": "Item"}]},
            {"probability": 0.05, "items": [{"item_id": 5, "quantity": 1, "item_type": "Item"}]}]}],
        "crafting_recipe_desc": [
            recipe(10, "Extract {0}", [(100, 1, "Cargo")], [(1, 4, "Item")]),
            recipe(11, "Forge {0}", [(1, 2, "Item"), (8, 1, "Item")], [(6, 1, "Item")], building=(25, 2)),
            recipe(12, "Craft {0}", [(2, 3, "Item")], [(3, 1, "Item")], building=(25, 2)),
            recipe(13, "Recraft {1}", [(3, 1, "Item"), (5, 1, "Item")], [(4, 1, "Item")]),
            recipe(14, "Forge {0}", [(7, 1, "Item")], [(2, 1, "Item")]),
            recipe(15, "Package {1} into {0}", [(2, 100, "Item")], [(9, 1, "Item")], building=(25, 1)),
            recipe(16, "Unpack {1}", [(9, 1, "Item")], [(2, 100, "Item")], building=(25, 1)),
            recipe(17, "Salvage {0}", [(102, 1, "Cargo")], [(2, 50, "Item")]),
            recipe(18, "Craft {0}", [(2, 1, "Item")], [(102, 1, "Cargo")]),
        ],
        "extraction_recipe_desc": [
            {"resource_id": 500, "extracted_item_stacks": [{"item_stack": {"item_id": 100, "quantity": 1, "item_type": "Cargo"}, "probability": 1.0}],
             "level_requirements": [{"skill_id": 5, "level": 10}], "tool_requirements": [{"tool_type": 4, "level": 2}]},
            {"resource_id": 501, "extracted_item_stacks": [{"item_stack": {"item_id": 7, "quantity": 1, "item_type": "Item"}, "probability": 1.0}],
             "level_requirements": [], "tool_requirements": []},
            {"resource_id": 502, "extracted_item_stacks": [], "level_requirements": [{"skill_id": 2, "level": 5}],
             "tool_requirements": [{"tool_type": 1, "level": 2}]},
        ],
        "resource_desc": [
            {"id": 500, "name": "Ore Vein", "tier": 2, "tag": "Ore Vein", "icon_asset_name": ""},
            {"id": 501, "name": "Old Anvil", "tier": 2, "tag": "Uncharted Resource", "icon_asset_name": ""},
            {"id": 502, "name": "Spruce Tree", "tier": 2, "tag": "Tree", "icon_asset_name": "",
             "on_destroy_yield": [{"item_id": 101, "quantity": 1, "item_type": "Cargo"}]},
        ],
        "skill_desc": [{"id": 2, "name": "Forestry"}, {"id": 5, "name": "Mining"}, {"id": 6, "name": "Smithing"}],
        "tool_type_desc": [{"id": 1, "name": "Axe"}, {"id": 4, "name": "Pickaxe"}],
        "building_type_desc": [{"id": 25, "name": "Smithing Station"}],
        "building_desc": [
            {"name": "Rough Smithing Station", "icon_asset_name": "GeneratedIcons/Other/GeneratedIcons/Other/Buildings/Smith",
             "functions": [{"function_type": 25, "level": 1, "crafting_slots": 1}]},
            {"name": "Simple Smithing Station", "icon_asset_name": "", "functions": [{"function_type": 25, "level": 2, "crafting_slots": 1}]},
            {"name": "Ancient Smithing Station", "icon_asset_name": "", "functions": [{"function_type": 25, "level": 2, "crafting_slots": 1}]},
        ],
        "building_repairs_desc": [{"cargo_id": 102, "repair_value": 340}],
    }


class RulesTest(unittest.TestCase):
    def setUp(self):
        self.d = core.build(base_tables(), "abc", "2026-11-26")
        self.k = lambda n: core.find_key(self.d["items"], n)

    def test_variants_grouped(self):
        axes = [v for v in self.d["items"].values() if v["n"] == "Axe"]
        self.assertEqual(len(axes), 1)
        self.assertEqual(axes[0]["r"], ["Common", "Rare"])

    def test_output_items_open(self):
        # « Ingot Output » s'ouvre en lingot : la recette « Forge » produit directement des lingots.
        best = self.d["recipes"][self.d["best"][self.k("Ingot")]]
        self.assertEqual(best["rn"], "Forge {0}")
        self.assertEqual([self.d["items"][i]["n"] for i, _ in best["in"]], ["Ore Piece", "Flux"])

    def test_rare_source_avoided(self):
        # L'arme de pirate (vieille enclume, ressource rare) coûte plus cher que le minerai.
        src = self.d["sources"][self.k("Pirate Weapon")]
        self.assertTrue(src[0]["rare"])
        best = self.d["recipes"][self.d["best"][self.k("Ingot")]]
        self.assertNotIn(self.k("Pirate Weapon"), [i for i, _ in best["in"]])

    def test_excluded_recipes(self):
        names = {r["rn"] for r in self.d["recipes"].values()}
        for word in ("Recraft", "Package", "Unpack", "Salvage"):
            self.assertFalse(any(n.startswith(word) for n in names), word)

    def test_trade_good_and_origins(self):
        it = self.d["items"]
        self.assertEqual(it[self.k("Flux")]["o"], "buy")
        self.assertEqual(it[self.k("Ingot")]["o"], "craft")
        self.assertEqual(it[self.k("Ore Chunk")]["o"], "gather")

    def test_felled_tree_gives_trunk(self):
        src = self.d["sources"][self.k("Trunk")]
        self.assertEqual(src[0]["res"], "Spruce Tree")
        self.assertTrue(src[0]["fd"])
        self.assertEqual(src[0]["tl"], ["Axe", 2])
        self.assertEqual(src[0]["s"], ["Forestry", 5])

    def test_building_names(self):
        best = self.d["recipes"][self.d["best"][self.k("Axe")]]
        self.assertEqual(best["b"]["n"], "Simple Smithing Station")   # pas « Ancient »
        rough = self.d["recipes"][self.d["best"][self.k("Ore Piece")]]
        self.assertEqual(rough["b"]["ic"], "GeneratedIcons/Other/Buildings/Smith")

    def test_stacks(self):
        it = self.d["items"]
        self.assertEqual(it[self.k("Ingot")]["st"], 10)
        self.assertEqual(it[self.k("Ore Chunk")]["st"], 1)
        self.assertIsNone(it[self.k("Axe")]["st"])

    def test_supplies(self):
        self.assertEqual(self.d["supplies"], [[self.k("Supply Crate"), 340]])

    def test_used_in(self):
        used = self.d["usedIn"][self.k("Ingot")]
        self.assertIn(self.k("Axe"), used)

    def test_helpers(self):
        self.assertEqual(core.crafts_needed(7, 6), 2)
        self.assertEqual(core.crafts_needed(0, 6), 0)
        self.assertEqual(core.normalize_icon("Items/HexCoin[,3,10,500]"), "Items/HexCoin")
        self.assertIsNone(core.normalize_icon("../../etc"))
        self.assertIsNone(core.normalize_icon("Items/../../x"))
        self.assertEqual(core.normalize_icon("Items/Tools/Axe.v2"), "Items/Tools/Axe.v2")


RAW = os.path.join(HERE, "..", "build", "raw")


@unittest.skipUnless(os.path.exists(os.path.join(RAW, "current.json")), "tables réelles absentes")
class RealDataTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cur = read_json(os.path.join(RAW, "current.json"))
        folder = os.path.join(RAW, cur["commit"])
        names = [f[:-5] for f in os.listdir(folder) if f.endswith(".json")]
        tables = {n: read_json(os.path.join(folder, n + ".json")) for n in names}
        cls.d = core.build(tables, cur["commit"], cur["updatedAt"])

    def names(self, rid):
        return [self.d["items"][i]["n"] for i, _ in self.d["recipes"][rid]["in"]]

    def best(self, name):
        return self.d["best"][core.find_key(self.d["items"], name)]

    def test_emarium_axe(self):
        ins = self.names(self.best("Emarium Axe"))
        self.assertIn("Pyrelite Axe", ins)
        self.assertIn("Emarium Ingot", ins)

    def test_emarium_ingot_from_ore(self):
        ins = self.names(self.best("Emarium Ingot"))
        self.assertIn("Molten Emarium", ins)
        self.assertIn("Metalworking Flux", ins)

    def test_planks_not_from_packages(self):
        rn = self.d["recipes"][self.best("Sturdy Plank")]["rn"]
        self.assertFalse(rn.startswith(("Unpack", "Salvage")))

    def test_counts(self):
        self.assertEqual(len(self.d["supplies"]), 64)
        self.assertGreater(len(self.d["best"]), 2000)


if __name__ == "__main__":
    unittest.main()
