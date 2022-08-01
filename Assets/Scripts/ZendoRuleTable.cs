using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ZendoRuleTable {
	// internal types

	enum FragmentType {
		RULE, NOUN, PREDICATE, GROUP, GROUP_PREDICATE
	}

	enum FragmentTag {
		SHAPE, COLOR, POSITION
	}

	class Entry {
		public Func<ZendoRuleFragment> Make;
		public float weight;
		public FragmentTag[] tags;
		public FragmentType[] childTypes;
		public int variantCount;
		public bool separateTags;

		public Entry(
			Func<ZendoRuleFragment> make,
			float weight,
			FragmentTag[] tags = null,
			FragmentType[] childTypes = null,
			int variantCount = 1,
			bool separateTags = false
		) {
			this.Make = make;
			this.weight = weight;
			this.tags = tags ?? new FragmentTag[] {};
			this.childTypes = childTypes ?? new FragmentType[] {};
			this.variantCount = variantCount;
			this.separateTags = separateTags;
		}
	}

	static Dictionary<FragmentType, List<Entry>> ruleDict = new Dictionary<FragmentType, List<Entry>>();

	public static ZendoRule RandomRule() {
		return (ZendoRule)RandomFragmentOfType(FragmentType.RULE, new HashSet<FragmentTag>());

		/*
		ZendoRule testRule = new GroupRule();

		testRule.children = new ZendoRuleFragment[2];

		testRule.children[0] = new ColorGroup();
		testRule.children[1] = new ContiguousGroupPredicate();

		return testRule;
		*/
	}

	private static ZendoRuleFragment RandomFragmentOfType(FragmentType type, HashSet<FragmentTag> usedTags) {
		List<Entry> options = ruleDict[type];

		Entry choice = null;
		float totalWeight = 0;

		foreach (Entry e in options) {
			// check tag conflicts
			bool conflict = false;
			foreach (FragmentTag t in e.tags) {
				if (usedTags.Contains(t)) {
					conflict = true;
					break;
				}
			}
			if (conflict)
				continue;

			// weighted random choice
			float nextWeight = totalWeight + e.weight;

			if (Random.value * nextWeight > totalWeight)
				choice = e;

			totalWeight = nextWeight;
		}

		if (choice == null)
			throw new Exception(string.Format("Couldn't find a legal fragment of type {0}", type));

		// append tags
		foreach (FragmentTag t in choice.tags) {
			usedTags.Add(t);
		}

		ZendoRuleFragment result = choice.Make();

		result.children = new ZendoRuleFragment[choice.childTypes.Length];
		for (int i = 0; i < choice.childTypes.Length; i++) {
			if (choice.separateTags) {
				HashSet<FragmentTag> childTags = new HashSet<FragmentTag>();

				if (usedTags.Contains(FragmentTag.POSITION)) {
					// position tag is global
					childTags.Add(FragmentTag.POSITION);
				}

				result.children[i] = RandomFragmentOfType(choice.childTypes[i], childTags);
			} else {
				result.children[i] = RandomFragmentOfType(choice.childTypes[i], usedTags);
			}
		}

		result.variant = UnityEngine.Random.Range(0, choice.variantCount);

		return result;
	}

	static ZendoRuleTable() {
		List<Entry> rules = new List<Entry>();
		rules.Add(new Entry(
			make: () => new UniversalRule(),
			weight: 10,
			childTypes: new[] {FragmentType.NOUN, FragmentType.PREDICATE}
		));
		rules.Add(new Entry(
			make: () => new ExistentialRule(),
			weight: 10,
			childTypes: new[] {FragmentType.NOUN, FragmentType.PREDICATE}
		));
		rules.Add(new Entry(
			make: () => new NegativeUniversalRule(),
			weight: 10,
			childTypes: new[] {FragmentType.NOUN, FragmentType.PREDICATE}
		));
		rules.Add(new Entry(
			make: () => new GroupRule(),
			weight: 20,
			childTypes: new[] {FragmentType.GROUP, FragmentType.GROUP_PREDICATE}
		));
		rules.Add(new Entry(
			make: () => new LineRule(),
			weight: 10,
			variantCount: 2,
			tags: new[] {FragmentTag.POSITION},
			childTypes: new[] {FragmentType.NOUN}
		));
		ruleDict.Add(FragmentType.RULE, rules);

		List<Entry> nouns = new List<Entry>();
		nouns.Add(new Entry(
			make: () => new ColorNoun(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.COLOR}
		));
		nouns.Add(new Entry(
			make: () => new ShapeNoun(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.SHAPE}
		));
		nouns.Add(new Entry(
			make: () => new RowNoun(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.POSITION}
		));
		nouns.Add(new Entry(
			make: () => new ColumnNoun(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.POSITION}
		));
		nouns.Add(new Entry(
			make: () => new EmptyCellNoun(),
			weight: 10,
			tags: new[] {FragmentTag.COLOR, FragmentTag.SHAPE}
		));
		ruleDict.Add(FragmentType.NOUN, nouns);

		List<Entry> predicates = new List<Entry>();
		predicates.Add(new Entry(
			make: () => new ColorPredicate(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.COLOR}
		));
		predicates.Add(new Entry(
			make: () => new ShapePredicate(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.SHAPE}
		));
		predicates.Add(new Entry(
			make: () => new RowPredicate(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.POSITION}
		));
		predicates.Add(new Entry(
			make: () => new ColumnPredicate(),
			weight: 20,
			variantCount: 3,
			tags: new[] {FragmentTag.POSITION}
		));
		predicates.Add(new Entry(
			make: () => new AdjacencyPredicate(),
			weight: 20,
			tags: new[] {FragmentTag.POSITION},
			childTypes: new[] {FragmentType.NOUN},
			separateTags: true
		));
		ruleDict.Add(FragmentType.PREDICATE, predicates);

		List<Entry> groups = new List<Entry>();
		groups.Add(new Entry(
			make: () => new ColorGroup(),
			weight: 10,
			tags: new[] {FragmentTag.COLOR}
		));
		groups.Add(new Entry(
			make: () => new ShapeGroup(),
			weight: 10,
			tags: new[] {FragmentTag.SHAPE}
		));
		groups.Add(new Entry(
			make: () => new IdenticalGroup(),
			weight: 10,
			tags: new[] {FragmentTag.COLOR, FragmentTag.SHAPE}
		));
		groups.Add(new Entry(
			make: () => new LineGroup(),
			weight: 20,
			variantCount: 2,
			tags: new[] {FragmentTag.POSITION}
		));
		groups.Add(new Entry(
			make: () => new AdjacentGroup(),
			weight: 10,
			tags: new[] {FragmentTag.POSITION}
		));
		ruleDict.Add(FragmentType.GROUP, groups);

		List<Entry> groupPreds = new List<Entry>();
		groupPreds.Add(new Entry(
			make: () => new ColorGroupPredicate(),
			weight: 20,
			tags: new[] {FragmentTag.COLOR}
		));
		groupPreds.Add(new Entry(
			make: () => new ShapeGroupPredicate(),
			weight: 20,
			tags: new[] {FragmentTag.SHAPE}
		));
		groupPreds.Add(new Entry(
			make: () => new ColorDistinctGroupPredicate(),
			weight: 20,
			tags: new[] {FragmentTag.COLOR}
		));
		groupPreds.Add(new Entry(
			make: () => new ShapeDistinctGroupPredicate(),
			weight: 20,
			tags: new[] {FragmentTag.SHAPE}
		));
		groupPreds.Add(new Entry(
			make: () => new ColorShapeGroupPredicate(),
			weight: 20,
			tags: new[] {FragmentTag.COLOR, FragmentTag.SHAPE}
		));
		groupPreds.Add(new Entry(
			make: () => new ContiguousGroupPredicate(),
			weight: 20,
			tags: new[] {FragmentTag.POSITION}
		));
		ruleDict.Add(FragmentType.GROUP_PREDICATE, groupPreds);
	}
}