using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		public int weight;
		public FragmentTag[] tags;
		public FragmentType[] childTypes;
		public int variantCount;
		public bool separateTags;

		public Entry(
			Func<ZendoRuleFragment> make,
			int weight,
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

	static ZendoRuleTable() {
		List<Entry> rules = new List<Entry>();
		rules.Add(new Entry(
			make: () => new UniversalRule(),
			weight: 7,
			childTypes: new[] {FragmentType.NOUN, FragmentType.PREDICATE}
		));
		ruleDict.Add(FragmentType.RULE, rules);

		List<Entry> nouns = new List<Entry>();
		nouns.Add(new Entry(
			make: () => new ColorNoun(),
			weight: 7,
			variantCount: 3,
			tags: new[] {FragmentTag.COLOR}));
		ruleDict.Add(FragmentType.NOUN, nouns);

		List<Entry> predicates = new List<Entry>();
		predicates.Add(new Entry(
			make: () => new ShapePredicate(),
			weight: 7,
			variantCount: 3,
			tags: new[] {FragmentTag.SHAPE}
		));
		ruleDict.Add(FragmentType.PREDICATE, predicates);
	}

	public static ZendoRule RandomRule() {
		return (ZendoRule)RandomFragmentOfType(FragmentType.RULE, new HashSet<FragmentTag>());
	}

	private static  ZendoRuleFragment RandomFragmentOfType(FragmentType type, HashSet<FragmentTag> usedTags) {
		List<Entry> options = ruleDict[type];

		//TODO
		Entry choice = options[0];

		ZendoRuleFragment result = choice.Make();

		result.children = new ZendoRuleFragment[choice.childTypes.Length];
		for (int i = 0; i < choice.childTypes.Length; i++) {
			result.children[i] = RandomFragmentOfType(choice.childTypes[i], usedTags);
		}

		result.variant = UnityEngine.Random.Range(0, choice.variantCount);

		return result;
	}
}