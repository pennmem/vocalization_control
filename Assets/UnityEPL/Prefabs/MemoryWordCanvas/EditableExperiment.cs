using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditableExperiment : MonoBehaviour
{
	public TextDisplayer textDisplayer;
    public UnityEngine.UI.InputField inputField;
    public ScriptedEventReporter scriptedEventReporter;
    public SoundRecorder soundRecorder;

    private string[] words;

	void Start()
	{
        UnityEPL.SetExperimentName("vocalization_control");
        LoadWords();
        StartCoroutine(RunExperiment());
	}

    private void LoadWords()
    {
        words = GetWordpoolLines("wordpool_en");
    }

    private IEnumerator RunExperiment()
    {
        textDisplayer.DisplayText("subject name prompt", "Please enter the subject name and then press enter.");
        yield return new WaitForSeconds(3f);
        textDisplayer.ClearText();
        inputField.gameObject.SetActive(true);
        inputField.Select();
        while (!Input.GetKeyDown(KeyCode.Return))
            yield return null;
        UnityEPL.AddParticipant(inputField.text);
        inputField.gameObject.SetActive(false);
        Cursor.visible = false;

        for (int i = 0; i < words.Length; i++)
        {
            yield return PerformTrial(i);
            if (Input.GetKey(KeyCode.Space))
            {
                textDisplayer.DisplayText("resting message", "Resting...");
                while (!Input.GetKeyDown(KeyCode.Return))
                    yield return null;
                textDisplayer.ClearText();
            }
        }

        //final recall
        string wav_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), "final_recall.wav");
        soundRecorder.StartRecording(wav_path);
        scriptedEventReporter.ReportScriptedEvent("final recall start", new Dictionary<string, object>());
        textDisplayer.DisplayText("final recall prompt", "******");
        yield return new WaitForSeconds(600f);
        textDisplayer.ClearText();
        scriptedEventReporter.ReportScriptedEvent("final recall stop", new Dictionary<string, object>());
        soundRecorder.StopRecording();

        //over
        textDisplayer.DisplayText("end message", "Yay, this... thing... is over!");
    }

    private IEnumerator PerformTrial(int word_index)
    {
        //orient
        textDisplayer.DisplayText("orientation", "+");
        yield return new WaitForSeconds(0.5f);
        textDisplayer.ClearText();

        //isi
        yield return new WaitForSeconds(Random.Range(0.4f, 0.6f));

        //stimulus
        string stimulus = words[word_index];
        scriptedEventReporter.ReportScriptedEvent("stimulus", new Dictionary<string, object> () { { "word", stimulus }, { "index", word_index } });
        textDisplayer.DisplayText("stimulus", stimulus);
        yield return new WaitForSeconds(1.6f);
        scriptedEventReporter.ReportScriptedEvent("stimulus cleared", new Dictionary<string, object>() { { "word", stimulus }, { "index", word_index } });
        textDisplayer.ClearText();

        //write .lst
        string lst_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), word_index.ToString() + ".lst");
        WriteAllLinesNoExtraNewline(lst_path, stimulus);

        //isi
        yield return new WaitForSeconds(Random.Range(0.8f, 1.2f));

        //recall
        string wav_path = System.IO.Path.Combine(UnityEPL.GetDataPath(), word_index.ToString() + ".wav");
        soundRecorder.StartRecording(wav_path);
        scriptedEventReporter.ReportScriptedEvent("recall start", new Dictionary<string, object>() { { "word", stimulus }, { "index", word_index } });
        textDisplayer.DisplayText("recall prompt", "******");
        yield return new WaitForSeconds(3f);
        textDisplayer.ClearText();
        scriptedEventReporter.ReportScriptedEvent("recall stop", new Dictionary<string, object>() { { "word", stimulus }, { "index", word_index } });
        soundRecorder.StopRecording();
    }

    private string[] GetWordpoolLines(string path)
    {
        string text = Resources.Load<TextAsset>(path).text;
        string[] lines = text.Split(new[] { '\r', '\n' });

        Shuffle<string>(new System.Random(), lines);

        return lines;
    }

    //thanks Matt Howells
    private void Shuffle<T> (System.Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1) 
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }

    //thanks Virtlink from stackoverflow
    protected static void WriteAllLinesNoExtraNewline(string path, params string[] lines)
    {
        if (path == null)
            throw new UnityException("path argument should not be null");
        if (lines == null)
            throw new UnityException("lines argument should not be null");

        using (var stream = System.IO.File.OpenWrite(path))
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
            {
                if (lines.Length > 0)
                {
                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.Write(lines[lines.Length - 1]);
                }
            }
        }
    }
}

static class RandomExtensions
{

}