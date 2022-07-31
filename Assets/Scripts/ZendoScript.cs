using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Random = UnityEngine.Random;

/*
 * Notes:
 * - Check to see how other modules with a palette behave re. interaction punch
 * - Find a better touchscreen-press sound
 * - Make screen objects 2D
 */

public class ZendoScript : MonoBehaviour {
	enum ModuleState {
		INACTIVE, EDIT, CHALLENGE, PASS, FAIL, DONE, ERROR
	}

	private readonly string[] SHAPE_KEYS = { "Circle", "Triangle", "Square" };

	public Light[] lights;

    public KMSelectable[] gridButtons;
    public KMSelectable[] paletteButtons;
    public KMSelectable yesButton;
    public KMSelectable noButton;
    public KMSelectable readyButton;

	public GameObject gridDisplay;

	public TextMesh gridText;
	public int gridTextColumns = 12;
	public TextMesh brushText;

	public Material[] shapeMaterials;

	public int maxChallenges = 5;

	int moduleId;
	static int totalModules = 0;

	ModuleState state = ModuleState.INACTIVE;

	ZendoRule rule = new DebugRule();
	ZendoSymbol[] grid = new ZendoSymbol[9];
	ZendoSymbol brush = new ZendoSymbol(0, 0);
	List<ZendoSymbol[]> challenges;
	int challengeIndex = 0;
	int activateRevealCounter = 0;

    void Start() {
        Init();

        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

	void Init() {
		moduleId = ++totalModules;

		// fix light range
		float scalar = transform.lossyScale.x;
		for (var i = 0; i < lights.Length; i++)
			lights[i].range *= scalar;
		
		for (int i = 0; i < gridButtons.Length; i++) {
			KMSelectable b = gridButtons[i];

			int j = i;

			b.OnInteract += delegate () { OnGridPress(j); return false; };
		}

		for (int i = 0; i < paletteButtons.Length; i++) {
			KMSelectable b = paletteButtons[i];

			int j = i;

			b.OnInteract += delegate () { OnPalettePress(j); return false; };
		}

		yesButton.OnInteract += delegate () { OnYNButtonPress(true); return false; }; 
		noButton.OnInteract += delegate () { OnYNButtonPress(false); return false; };
		readyButton.OnInteract += delegate () { OnReadyPress(); return false; };

		NewRule();
		UpdateDisplay();
    }

    void ActivateModule() {
		state = ModuleState.EDIT;

		UpdateDisplay();

		StartCoroutine(ActivateReveal());
    }

	void UpdateDisplay() {
		bool valid = false;

		if (state != ModuleState.ERROR)
			valid = EvaluateRule();

		// grid
		switch (state) {
		case ModuleState.PASS:
		case ModuleState.FAIL:
			gridDisplay.SetActive(false);
			gridText.gameObject.SetActive(true);
			SetGridText(rule.GetDescription());
			break;
		case ModuleState.ERROR:
			gridDisplay.SetActive(false);
			gridText.gameObject.SetActive(true);
			SetGridText("Error :( Press Ready to solve");
			break;
		case ModuleState.DONE:
			gridDisplay.SetActive(false);
			gridText.gameObject.SetActive(false);
			break;
		default:
			gridDisplay.SetActive(true);
			gridText.gameObject.SetActive(false);
			for (int i = 0; i < grid.Length; i++) {
				ZendoSymbol symbol = grid[i];

				if (i >= activateRevealCounter)
					symbol = ZendoSymbol.Empty;

				SetShapeIndicator(gridButtons[i].gameObject, symbol);
			}
			break;
		}

		// lights
		switch (state) {
		case ModuleState.INACTIVE:
			SetButtonLights(0);
			break;
		case ModuleState.EDIT:
			if (valid) {
				SetButtonLights(0);
			} else {
				SetButtonLights(1);
			}
			break;
		case ModuleState.CHALLENGE:
			SetButtonLights(2);
			break;
		default:
			SetButtonLights(-1);
			break;
		}

		// brush
		// TODO challenge counter
		switch (state) {
		case ModuleState.EDIT:
			paletteButtons[6].gameObject.SetActive(true);
			brushText.gameObject.SetActive(false);
			SetShapeIndicator(paletteButtons[6].gameObject, brush);
			break;
		case ModuleState.CHALLENGE:
			paletteButtons[6].gameObject.SetActive(false);
			brushText.gameObject.SetActive(true);
			brushText.text = string.Format("{0}/{1}", challengeIndex, challenges.Count);
			break;
		default:
			paletteButtons[6].gameObject.SetActive(false);
			brushText.gameObject.SetActive(false);
			break;
		}
	}

	bool EvaluateRule() {
		try {
			return rule.Evaluate(grid);
		} catch (Exception e) {
			Log(e.ToString());
			SwitchToError();

			throw e;
		}
	}

	public List<ZendoSymbol[]> GenerateExamples(int numPositive, int numNegative) {
		List<ZendoSymbol[]> pos = new List<ZendoSymbol[]>();
		List<ZendoSymbol[]> neg = new List<ZendoSymbol[]>();

		int timeout = 1000;

		while (timeout > 0 && (pos.Count < numPositive || neg.Count < numNegative)) {
			ZendoSymbol[] g = new ZendoSymbol[9];

			int fillCount = Random.Range(3, 10);

			for (int i = 0; i < g.Length; i++) {
				if (Random.value < ((double)fillCount) / ((double)(9 - i))) {
					g[i] = new ZendoSymbol(Random.Range(0, 3), Random.Range(0, 3));
					fillCount--;
				} else {
					g[i] = ZendoSymbol.Empty;
				}
			}

			bool valid = rule.Evaluate(g);

			if (valid) {
				if (pos.Count < numPositive)
					pos.Add(g);
			} else {
				if (neg.Count < numNegative)
					neg.Add(g);
			}

			timeout--;
		}

		if (pos.Count == numPositive && neg.Count == numNegative) {
			//Debug.LogFormat("Generated examples after {0} tries", 1000 - timeout);
		} else {
			Log("Warning: Failed to generate examples");
		}

		pos.AddRange(neg);
		return pos;
	}

	public void DisplayExample(bool valid) {
		List<ZendoSymbol[]> examples;

		if (valid) {
			examples = GenerateExamples(1, 0);
		} else {
			examples = GenerateExamples(0, 1);
		}

		if (examples.Count == 1)
			grid = examples[0];
	}

	public void InitChallenge() {
		challenges = GenerateExamples(maxChallenges - 1, maxChallenges - 1);
		challenges.Shuffle();
		challenges.RemoveRange(maxChallenges, challenges.Count - maxChallenges);

		state = ModuleState.CHALLENGE;
		challengeIndex = 0;

		// This passes the list by reference but it's single use anyway so it's fine
		grid = challenges[challengeIndex];

		UpdateDisplay();
	}

	public IEnumerator ActivateReveal() {
		while (activateRevealCounter < grid.Length) {
			yield return new WaitForSeconds(0.05f);
			activateRevealCounter++;
			UpdateDisplay();
		}
	}

	public void NewRule() {
		try {
			rule = ZendoRuleTable.RandomRule();
			LogFormat("The rule is: {0}", rule.GetDescription());
			DisplayExample(true);
		} catch (Exception e) {
			Log(e.ToString());
			SwitchToError();

			throw e;
		}
	}

	public IEnumerator SwitchToPass() {
		Log("Solved!");

		GetComponent<KMBombModule>().HandlePass();
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);

		state = ModuleState.PASS;
		UpdateDisplay();

		yield return new WaitForSeconds(5);

		state = ModuleState.DONE;
		UpdateDisplay();
	}

	public IEnumerator SwitchToFail() {
		GetComponent<KMBombModule>().HandleStrike();

		state = ModuleState.FAIL;
		UpdateDisplay();

		yield return new WaitForSeconds(3);

		NewRule();
		state = ModuleState.EDIT;
		UpdateDisplay();
	}

	public void SwitchToError() {
		state = ModuleState.ERROR;
		UpdateDisplay();
	}

	void OnGridPress(int n) {
		OnScreenPress();

		if (state == ModuleState.EDIT) {
			grid[n] = brush;
		}

		UpdateDisplay();
	}

	void OnPalettePress(int n) {
		if (n == 6) {
			// Erase button
			OnScreenPress();
			brush.empty = true;
		} else if (n < 3) {
			OnButtonPress();
			brush.shape = n;
		} else {
			OnButtonPress();
			brush.color = n - 3;
		}

		UpdateDisplay();
	}

	void OnYNButtonPress(bool yes) {
		OnButtonPress();

		switch (state) {
		case ModuleState.EDIT:
			DisplayExample(yes);
			break;
		case ModuleState.CHALLENGE:
			// submit answer
			// TODO
			bool actual = EvaluateRule();
			if (yes == actual) {
				challengeIndex++;

				if (challengeIndex == challenges.Count) {
					StartCoroutine(SwitchToPass());
				} else {
					// This passes the list by reference but it's single use anyway so it's fine
					grid = challenges[challengeIndex];
				}
			} else {
				string[] gridLog = ZendoSymbol.GridToString(grid).Split('\n');

				Log("The grid was:");
				foreach (string line in gridLog) {
					Log(line);
				}
				LogFormat("This was {0} but you pressed {1} - Strike!", actual ? "valid" : "invalid", yes ? "Y" : "N");

				StartCoroutine(SwitchToFail());
			}
			break;
		}

		UpdateDisplay();
	}

	void OnReadyPress() {
		OnButtonPress();

		switch (state) {
		case ModuleState.EDIT:
			state = ModuleState.CHALLENGE;
			InitChallenge();
			break;
		case ModuleState.CHALLENGE:
			state = ModuleState.EDIT;
			break;
		case ModuleState.ERROR:
			GetComponent<KMBombModule>().HandlePass();
			break;
		}

		UpdateDisplay();	
	}

	void SetButtonLights(int index) {
		bool yesOn = (index == 0);
		bool noOn = (index == 1);
		bool readyOn = (index == 2);

		yesButton.transform.Find("GlowOn").gameObject.SetActive(yesOn);
		yesButton.transform.Find("GlowOff").gameObject.SetActive(!yesOn);

		noButton.transform.Find("GlowOn").gameObject.SetActive(noOn);
		noButton.transform.Find("GlowOff").gameObject.SetActive(!noOn);

		readyButton.transform.Find("GlowOn").gameObject.SetActive(readyOn);
		readyButton.transform.Find("GlowOff").gameObject.SetActive(!readyOn);
	}

	void SetShapeIndicator(GameObject obj, ZendoSymbol symbol) {
		obj = obj.transform.Find("Shapes").gameObject;

		if (symbol.empty) {
			obj.SetActive(false);
			return;
		}

		obj.SetActive(true);

		int shape = symbol.shape;
		int color = symbol.color;

		for (int i = 0; i < 3; i++) {
			string currentKey = SHAPE_KEYS[i];
			Transform currentShape = obj.transform.Find(currentKey);

			if (!currentShape) {
				LogFormat("Error: Couldn't find shape {0} in indicator", currentKey);
				continue;
			}

			if (shape == i) {
				currentShape.gameObject.SetActive(true);
				currentShape.GetComponent<MeshRenderer>().material = shapeMaterials[color];
			} else {
				currentShape.gameObject.SetActive(false);
			}
		}
	}

	void SetGridText(string text) {
		string[] tokens = text.Split(' ');
		StringBuilder sb = new StringBuilder();

		int lineLength = 0;

		foreach (string token in tokens) {
			if (lineLength + 1 + token.Length > gridTextColumns) {
				sb.Append("\n");
				lineLength = 0;
			}

			if (lineLength > 0) {
				sb.Append(" ");
				lineLength++;
			}

			sb.Append(token);
			lineLength += token.Length;
		}

		gridText.text = sb.ToString();
	}

	void OnScreenPress() {
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
	}

	void OnButtonPress() {
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable>().AddInteractionPunch();
	}

	public KMSelectable[] ProcessTwitchCommand(string command) {
		//TODO
		return new KMSelectable[] {};

	}

	public void TwitchHandleForcedSolve() {
		//TODO
	}

	void Log(string s) {
		Debug.LogFormat("[Zendo #{0}] {1}", moduleId, s);
	}

	void LogFormat(string format, params object[] args) {
		string s = string.Format(format, args: args);
		Debug.LogFormat("[Zendo #{0}] {1}", moduleId, s);
	}

	//TODO LogWarning and LogError
}
