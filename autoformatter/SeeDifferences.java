import java.io.File;
import java.io.FileReader;
import java.io.FilenameFilter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Scanner;


public class SeeDifferences {

    private static File inputDir = new File(""); //old file folder
    private static File outputDir = new File(""); //new file folder

    //Tries to find the exact line differences between two dialogue sections
    public static void main(String[] args) {
        try {
            FilenameFilter textFilterA = new FilenameFilter() {
                public boolean accept(File inputDir, String name) {
                    return name.toLowerCase().endsWith(".bytes");
                }
            };
    
            File[] filesA = inputDir.listFiles(textFilterA);
            System.out.println(filesA.length+" files to check (old)");

            FilenameFilter textFilterB = new FilenameFilter() {
                public boolean accept(File outputDir, String name) {
                    return name.toLowerCase().endsWith(".bytes");
                }
            };
    
            File[] filesB = outputDir.listFiles(textFilterB);
            System.out.println(filesB.length+" files to check (new)");

            int j = 0;
            for (int i = 0; i < filesA.length; i++) {
                if (filesA[i].getName().equals(filesB[i].getName())) {
                    //System.out.println("[Start Check] Checking " + filesA[i].getCanonicalPath());
                    filecheck(filesA[i],filesB[i]);
                } else {
                    j++;
                }
                if (j > filesB.length - 1) {
                    break;                    
                }
            }

        } catch (Exception e) {
            System.out.println(e);
            System.out.println("Error");
        } finally {
            System.out.println("Processing done or stopped.");
        }        
    }

    public static void filecheck(File a, File b) {
        ArrayList<String> dialogueListA = new ArrayList<String>(); 
        ArrayList<String> dialogueListB = new ArrayList<String>(); 
        

        try {
            //System.out.println("    [Old] Checking file "+a.getCanonicalPath());
            Scanner input = new Scanner(a);
            //get file
            while(input.hasNextLine()){
              dialogueListA.add(input.nextLine());
            }
            input.close();
            //System.out.println("    [New] Checking file "+b.getCanonicalPath());
            Scanner inputB = new Scanner(b);
            //get file
            while(inputB.hasNextLine()){
              dialogueListB.add(inputB.nextLine());
            }
            inputB.close();
        }
          catch(Exception e){
            System.out.println("Error reading or parsing "+a.getName());
        }
        try {
            if (dialogueListA.size() != dialogueListB.size()) {        
                System.out.println(a.getCanonicalPath()+"        Different lengths: "+dialogueListA.size()+" vs "+dialogueListB.size());
            }    
        } catch (IOException e) {
            System.out.println("Error");
        }

        for (int i = 0; i < dialogueListA.size(); i++) {
            if (i > dialogueListB.size() - 1) {
                break;
            }
            try {
                if (!dialogueListA.get(i).equals(dialogueListB.get(i))) {
                    System.out.println(a.getCanonicalPath()+"        Line "+i+" is different.");
                }    
            } catch (IOException e) {
                System.out.println("Error");
            }    
        }
    }
}