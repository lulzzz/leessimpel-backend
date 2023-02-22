import Cocoa
import Vision

guard CommandLine.arguments.count > 1 else {
    print("Please provide a valid image path.")
    exit(1)
}

let imagePath = CommandLine.arguments[1]

guard let image = NSImage(contentsOfFile: imagePath) else {
    print("Failed to load image from path: \(imagePath)")
    exit(1)
}

let request = VNRecognizeTextRequest { request, error in
    guard let observations = request.results as? [VNRecognizedTextObservation] else {
        print("Failed to recognize text in image.")
        exit(1)
    }
    
    let text = observations.compactMap { observation -> String? in
        guard let bestCandidate = observation.topCandidates(1).first else { return nil }
        return bestCandidate.string + "\n"
    }.joined(separator: "\n")
    
    if CommandLine.arguments.count > 2 {
        let outputPath = CommandLine.arguments[2]
        do {
            try text.write(toFile: outputPath, atomically: true, encoding: .utf8)
        } catch {
            print("Failed to write text observations to output file: \(outputPath)")
            exit(1)
        }
    } else {
        print(text)
    }
}

let handler = VNImageRequestHandler(data: image.tiffRepresentation!, options: [:])

do {
    try handler.perform([request])
} catch {
    print("Failed to perform text recognition on image.")
    exit(1)
}