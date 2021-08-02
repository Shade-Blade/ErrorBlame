import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.FilenameFilter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.regex.*;
import java.util.Scanner;

public class DialogueImprover {
    //This is supposed to Google Translate the dialogue automatically
    //However, just translating the file would break things (there are a lot of tags that could break)
    //So I need to parse the file so that only the actual text gets translated
    //Parsing rules:
    //  Each piece of dialogue is on its own line (line break: |line| or |halfline|)
    //  Tags are in lines: ||
    //  However, within the lines, option text is after @ and before ,
    //  Before parsing: replace |line| with line break char
    //      ...then revert the changes
    //  |next| is used for multi-box dialogue
    //  global tags are in between @s outside of the lines
    //  has some singleton @ and {
    //  has }121} like tags

    //Might need to add special rules to prevent tags from breaking sentences
    //(*this would make the text make "more sense")
    //method 1: treat each thing separately
    //  will lead to non-sequiturs
    //method 2: remove all tags and manually replace them
    //  tedious but gives more logical results

    //ok unfortunately there doesn't seem to be an automatic way to google translate stuff
    //
    //tag replacement stuff: replace |button| with "Z"
    //  if there are multiple different buttons, replace second one with "J"
    //language list: english -> chinese -> slovak -> korean -> welsh -> igbo -> basque -> tamil -> english

    //the B list
    //  use after the A list
    //               english -> corsican -> hungarian -> uyghur -> indonesian -> arabic -> english

    //NEW PLAN
    //Google translate API can't be used directly since I'm not going through the process of getting access to it
    //The main work that takes all the time is parsing stuff manually
    //Time to automate it

    /*
    public static String inputFile = "input\\CardDialogue.txt";
    public static String outputFile = "input\\CardDialogueRevised.txt"; //not used yet
    */

    private static File dir = new File("");     //Folder in which the files should be translated
    //private static File outputDir = new File("\\dialogues0 - Improved V1\\mapsAutoFormatted");
    
    //only modify the map files
    //the text ends with .bytes, the meta files with .bytes.meta

    //contains each unprocessed line of dialogue (not parsed through rules above)

    private static String tagFinder = "/(\\|[a-z,\\-\\d{.\\s]+(@[A-Za-z\\-\\d]+)[a-z,\\d\\-{.\\s]*\\|)|(\\|[a-z,\\d\\-{.\\s]+\\|)|(\\|[A-Za-z,\\d\\-{.@\\s]+\\|)|(@[A-Z]+@)|@|\\{|(}\\d\\-+})|/g";

    //Group 1 = Total || tag of a tag with one @
    //Group 2 = @ tag within ||
    //Group 3 = || tag and contents
    //Group 4 = || tag and contents with multiple @ tags within
    //Group 5 = @@ tag
    //Group 6 = }} tag
    
    //put each tag on one line and each line has a gap line

    //Finds all tags, } tags, { and @ instances
    //Fails when multiple @ tags in a ||
        
    //Finds all @ tags inside the || tag
    private String complexTagFinder = "/@[a-zA-z}\\s]+/g";

    public static void reformat(File file) throws IOException {
        ArrayList<String> dialogueList = new ArrayList<String>(); 
        ArrayList<String> dialogueListB = new ArrayList<String>();
        String dialogueListC = ""; //dialogueListB as one string (each list element separated by \n);
        ArrayList<String> tagList = new ArrayList<String>(); //list of tags as a string

        try {
            Scanner input = new Scanner(file);
            //get file
            while(input.hasNextLine()){
              dialogueList.add(input.nextLine());
            }
            input.close();
          }
          catch(Exception e){
            System.out.println("Error reading or parsing "+file.getName());
          }

        
        //dialogueList = new ArrayList<String>(Arrays.asList(inputText));
        System.out.println("Detected "+dialogueList.size()+" lines! Parsing...");
        System.out.println("\n");
        String[] splitDialogue; // dialogue split by tags (this messes up when the tags have text within, but that's only used for option text)

        int maxChar = 625; //trying to find the maximum characters that doesn't surpass Google Translates 3900 char limit
                          //though the limit is pretty random since it's based of how many characters the translated version is
        int runningCharTotal = 0;

        int baseIndex = 0;

        while (baseIndex < dialogueList.size()) {
            //combine stuff if possible
            runningCharTotal = 0;
            String temp = "";
            do { 
                temp += dialogueList.get(baseIndex);
                temp += "\n";
                runningCharTotal += dialogueList.get(baseIndex).length();
                baseIndex++;
            } while (baseIndex < dialogueList.size() && dialogueList.get(baseIndex).length() + runningCharTotal < maxChar); //if first line is longer than max, it'll just be one line
            System.out.print(" ct = "+runningCharTotal+". ");
            dialogueListB.add(temp);

            //find all the tags in temp
            Matcher m = Pattern.compile(tagFinder).matcher(temp);
            tagList = new ArrayList<String>(); //clear out the tag list
            while (m.find()) {
              tagList.add(m.group());
            }

            splitDialogue = dialogueListB.get(dialogueListB.size() - 1).split(tagFinder);
            dialogueListB.add("--formatted--");
            for (int j = 0; j < splitDialogue.length; j++) {
                dialogueListB.add(splitDialogue[j]);
            }
            dialogueListB.add("--tags--"); //separator
            for (int k = 0; k < tagList.size(); k++) {
                dialogueListB.add(tagList.get(k));
            }
            dialogueListB.add("--line--"); //separator
        }
        //now we put everything from dialogueListB back into our target file
        for (int i = 0; i < dialogueListB.size(); i++) {
            dialogueListC += dialogueListB.get(i);
            if (i < dialogueListB.size()) {
                dialogueListC += "\n";
            }
        }
 
        System.out.println("Formatting complete, now writing to file");


        FileWriter writer = new FileWriter(file);

        
        try {
            writer.write(dialogueListC);
        } catch (IOException e) {
            System.out.println("Error in writing file: "+file.getName()+". Please check file to see if it is corrupted.");

        } finally {
            writer.close();
        }
        
    }

    public static int check2 = 72;

    public static linecheck(File file) {
        try {
            Scanner input = new Scanner(file);
            //get file
            while(input.hasNextLine()){
              dialogueList.add(input.nextLine());
            }
            input.close();
          }
          catch(Exception e){
            System.out.println("Error reading or parsing "+file.getName());
          }
    }

    public static void main(String[] args) {
            /*
            Scanner input = new Scanner(System.in);
            System.out.println("Please input target text (use STOP to cancel early)");
            String inputText = input.nextLine(); 
            if (inputText == "STOP") {
                
            }   
            */
        try {
            FilenameFilter textFilter = new FilenameFilter() {
                public boolean accept(File dir, String name) {
                    return name.toLowerCase().endsWith(".bytes");
                }
            };
    
            File[] files = dir.listFiles(textFilter);
            System.out.println(files.length+" files to format.");
            for (File file : files) {
                if (file.isDirectory()) {
                    System.out.print("directory:");
                } else {
                    System.out.print("     file:");
                    reformat(file);
                    //break; //debug
                }
                System.out.println(file.getCanonicalPath());
            }

        } catch (IOException e) {
            System.out.println(e);
        } catch (Exception e) {
            System.out.println(e);
            System.out.println("Error in parsing and reformatting! Look at log to determine which files are intact.");
        } finally {
            System.out.println("Processing done or stopped.");
        }
    }
}