import imgaug as ia
from imgaug import augmenters as iaa
import cv2
import sys
import os
from PIL import Image
from lxml import etree
from xml.dom import minidom
from shutil import copyfile
from xml.dom import minidom
import glob
from scipy import ndimage


##############################################################
#
# JH - adapted from code found at https://imgaug.readthedocs.io/en/latest/source/examples_bounding_boxes.html
# Take images with their annotations, augment them, and save them
# with new bounding box
# Supports multiple bounding boxes
#
##############################################################

# Generate new output annotation xml file, describing the new augmented bounding box
def GenerateXML(baseFileName, fullPath, width, height, folder, x1s, y1s, x2s, y2s):

    root     = etree.Element('annotation')
    _folder   = etree.SubElement( root, 'folder').text = folder
    _filename = etree.SubElement( root, 'filename').text = baseFileName
    path     = etree.SubElement( root, 'path').text = fullPath
    source   = etree.SubElement( root, 'source')
    database = etree.SubElement( source, 'database').text = 'Unknown'
    size = etree.SubElement( root, 'size')
    im_width = etree.SubElement( size, 'width').text = str(width)
    im_height = etree.SubElement( size, 'height').text = str(height)
    im_depth = etree.SubElement( size, 'depth').text = str(3)
    segmented = etree.SubElement( root, 'segmented').text = str(0)

    for i in range(0, len(x1s)):
        obj = etree.SubElement( root, 'object')
        obj_name = etree.SubElement( obj, 'name').text = folder
        obj_pose = etree.SubElement( obj, 'pose').text = 'Unspecified'
        obj_truncated = etree.SubElement( obj, 'truncated').text = 'Unspecified'
        obj_difficult = etree.SubElement( obj, 'difficult').text = 'Unspecified'

        bndbox = etree.SubElement( obj, 'bndbox')
        bndbox_xmin = etree.SubElement( bndbox, 'xmin').text = str(int(x1s[i]))
        bndbox_ymin = etree.SubElement( bndbox, 'ymin').text = str(int(y1s[i]))
        bndbox_xmax = etree.SubElement( bndbox, 'xmax').text = str(int(x2s[i]))
        bndbox_ymax = etree.SubElement( bndbox, 'ymax').text = str(int(y2s[i]))

    tree = etree.ElementTree(root)
    return tree

def SaveAnnotation(outputDir, tree, idx):
    # Save xml annotations
    xml_filename = str(idx) + '.xml'
    annotationPath = outputDir + xml_filename
    print("saving annotation at " + annotationPath)
    tree.write(annotationPath, pretty_print=True, xml_declaration=True,   encoding="utf-8")
def sortKeyFunc(s):
    return int(os.path.basename(s)[:-4])

def clearDir(folder):
    for the_file in os.listdir(folder):
        file_path = os.path.join(folder, the_file)
        try:
            if os.path.isfile(file_path):
                os.unlink(file_path)
            #elif os.path.isdir(file_path): shutil.rmtree(file_path)
        except Exception as e:
            print(e)


outputDir = sys.argv[1]
inputDir = sys.argv[2]
label = sys.argv[3]
#clearDir(outputDir + "test/")
#clearDir(outputDir + "train/")
#clearDir(outputDir)


# read in images (jpgs only)
print("looking in " + inputDir)
fps = glob.glob(inputDir + "/*.jpg")
fps.sort(key=sortKeyFunc)
images = [ndimage.imread(fp, mode="RGB") for fp in fps]

# read in their xml annotations
annotations =  glob.glob(inputDir + "/*.xml")
annotations.sort(key=sortKeyFunc)

# set numaugs
numaugs = 25

total = len(images) * numaugs

print("found " + str(len(images)) + " images")
print(str(numaugs) + " augs")
print("totalling " + str(total) + " images")


numTestImages = total / 10
print(str(numTestImages) + " test images")
print(str(total - numTestImages) + " train images")

print("placing output images in " + outputDir)

for i in range(0, len(images)):

    print("working on image:" + str(i))
    # parse the input xml annotation file
    xmldoc = minidom.parse(annotations[i])

    # get the current image 
    image = images[i]

    # input bbs
    x1s = []
    x2s = []
    y1s = []
    y2s = []
    for l in range(0, len(xmldoc.getElementsByTagName('xmin'))):
        x1s.append(int(xmldoc.getElementsByTagName('xmin')[l].firstChild.nodeValue))
        y1s.append(int(xmldoc.getElementsByTagName('ymin')[l].firstChild.nodeValue))
        x2s.append(int(xmldoc.getElementsByTagName('xmax')[l].firstChild.nodeValue))
        y2s.append(int(xmldoc.getElementsByTagName('ymax')[l].firstChild.nodeValue))

    width   =    int(xmldoc.getElementsByTagName('width')[0].firstChild.nodeValue)
    height  =    int(xmldoc.getElementsByTagName('height')[0].firstChild.nodeValue)

    # sometimes memory gets overloaded when working with large images, so set a size threshold
    if(width > 1500 or height > 1500):
        print("dims too large, skipping")
        continue

    # not sure what this does, so best not to touch it.....
    ia.seed(1)

    # seq seems to describe the augmentation, maybe put this is in a function 
    sometimes = lambda aug: iaa.Sometimes(0.5, aug)
    seq = iaa.Sequential(
        [
            # apply the following augmenters to most images
            # crop images by -5% to 10% of their height/width
            sometimes(iaa.CropAndPad(
                percent=(-0.05, 0.1),
                pad_mode=ia.ALL,
                pad_cval=(0, 255)
            )),
            sometimes(iaa.Affine(
                scale={"x": (0.8, 1.2), "y": (0.8, 1.2)}, # scale images to 80-120% of their size, individually per axis
                translate_percent={"x": (-0.2, 0.2), "y": (-0.2, 0.2)}, # translate by -20 to +20 percent (per axis)
                rotate=(-45, 45), # rotate by -45 to +45 degrees
                shear=(-16, 16), # shear by -16 to +16 degrees
                order=[0, 1], # use nearest neighbour or bilinear interpolation (fast)
                cval=(0, 255), # if mode is constant, use a cval between 0 and 255
                mode=ia.ALL # use any of scikit-image's warping modes (see 2nd image from the top for examples)
            )),
            # execute 0 to 5 of the following (less important) augmenters per image
            # don't execute all of them, as that would often be way too strong
            iaa.SomeOf((0, 5),
                [
                    sometimes(iaa.Superpixels(p_replace=(0, 1.0), n_segments=(20, 200))), # convert images into their superpixel representation
                    iaa.OneOf([
                        iaa.GaussianBlur((0, 3.0)), # blur images with a sigma between 0 and 3.0
                        iaa.AverageBlur(k=(2, 7)), # blur image using local means with kernel sizes between 2 and 7
                        iaa.MedianBlur(k=(3, 11)), # blur image using local medians with kernel sizes between 2 and 7
                    ]),
                    iaa.Sharpen(alpha=(0, 1.0), lightness=(0.75, 1.5)), # sharpen images
                    iaa.Emboss(alpha=(0, 1.0), strength=(0, 2.0)), # emboss images
                    # search either for all edges or for directed edges,
                    # blend the result with the original image using a blobby mask
                    iaa.SimplexNoiseAlpha(iaa.OneOf([
                        iaa.EdgeDetect(alpha=(0.5, 1.0)),
                        iaa.DirectedEdgeDetect(alpha=(0.5, 1.0), direction=(0.0, 1.0)),
                    ])),
                    iaa.AdditiveGaussianNoise(loc=0, scale=(0.0, 0.05*255), per_channel=0.5), # add gaussian noise to images
                    iaa.OneOf([
                        iaa.Dropout((0.01, 0.1), per_channel=0.5), # randomly remove up to 10% of the pixels
                        iaa.CoarseDropout((0.03, 0.15), size_percent=(0.02, 0.05), per_channel=0.2),
                    ]),
                    iaa.Invert(0.05, per_channel=True), # invert color channels
                    iaa.Add((-10, 10), per_channel=0.5), # change brightness of images (by -10 to 10 of original value)
                    iaa.AddToHueAndSaturation((-20, 20)), # change hue and saturation
                    # either change the brightness of the whole image (sometimes
                    # per channel) or change the brightness of subareas
                    iaa.OneOf([
                        iaa.Multiply((0.5, 1.5), per_channel=0.5),
                        iaa.FrequencyNoiseAlpha(
                            exponent=(-4, 0),
                            first=iaa.Multiply((0.5, 1.5), per_channel=True),
                            second=iaa.ContrastNormalization((0.5, 2.0))
                        )
                    ]),
                    iaa.ContrastNormalization((0.5, 2.0), per_channel=0.5), # improve or worsen the contrast
                    iaa.Grayscale(alpha=(0.0, 1.0)),
                    sometimes(iaa.ElasticTransformation(alpha=(0.5, 3.5), sigma=0.25)), # move pixels locally around (with random strengths)
                    sometimes(iaa.PiecewiseAffine(scale=(0.01, 0.05))), # sometimes move parts of the image around
                    sometimes(iaa.PerspectiveTransform(scale=(0.01, 0.1)))
                ],
                random_order=True
            )
        ],
        random_order=True
    )


    # Start working on the augmentations!
    for j in range(0, numaugs):

       
        
       
        # get bounding boxes and put them in a bounding_boxes array just because this is the format it wants it in.. 
        x1s_out = []
        x2s_out = []
        y1s_out = []
        y2s_out = []
        myBB = []
        bbs = ia.BoundingBoxesOnImage(myBB, shape=image.shape)
        for b in range(0, len(x1s)):
            myBB.append(ia.BoundingBox(x1=x1s[b], y1=y1s[b], x2=x2s[b], y2=y2s[b]))
        bbs = ia.BoundingBoxesOnImage(myBB, shape=image.shape)

        # Make our sequence deterministic.
        # We can now apply it to the image and then to the BBs and it will
        # lead to the same augmentations.
        # IMPORTANT: Call this once PER BATCH, otherwise you will always get the
        # exactly same augmentations for every batch!
        seq_det = seq.to_deterministic()

        # Augment BBs and images.
        # As we only have one image and list of BBs, we use
        # [image] and [bbs] to turn both into lists (batches) for the
        # functions and then [0] to reverse that. In a real experiment, your
        # variables would likely already be lists.
        image_aug = seq_det.augment_images([image])[0]
        bbs_aug = seq_det.augment_bounding_boxes([bbs])[0]

        # print coordinates before/after augmentation (see below)
        # use .x1_int, .y_int, ... to get integer coordinates
        for k in range(len(bbs.bounding_boxes)):
            before = bbs.bounding_boxes[k]
            after = bbs_aug.bounding_boxes[k]

            # sometimes the augmentation leads the bb coords to be outside the bounds of the image
            # not sure why it doesn't handle this but i've added this here which fixes it]
            if after.x1 > width : after.x1 = width
            if after.x1 < 0 : after.x1 = 0
            if after.y1 > height: after.y1 = height
            if after.y1 < 0 : after.y1 = 0
            if after.x2 > width : after.x2 = width
            if after.x2 < 0 : after.x2 = 0
            if after.y2 > height: after.y2 = height
            if after.y2 < 0 : after.y2 = 0
          
            # if(after.x1 > width ):
            #     after.x1 = width
            # if(after.x1 < 0 ):
            #     after.x1 = 0
            # if(after.y1 > height):
            #     after.y1 = height
            # if(after.y1 < 0):
            #     after.y1 = 0
            # if(after.x2 > width ):
            #     after.x2 = width
            # if(after.x2 < 0 ):
            #     after.x2 = 0
            # if(after.y2 > height ):
            #     after.y2 = height
            # if(after.y2 < 0 ):
            #     after.y2 = 0

            # output bb coords
            x1s_out.append(after.x1)
            x2s_out.append(after.x2)
            y1s_out.append(after.y1)
            y2s_out.append(after.y2)
            
            # more details bb debugging, uncomment if you give a crap
            # print("img " +  str(idx) + " BB %d: (%.4f, %.4f, %.4f, %.4f) -> (%.4f, %.4f, %.4f, %.4f)" % (
            # j,
            # before.x1, before.y1, before.x2, before.y2,
            # after.x1, after.y1, after.x2, after.y2)
            # )

        # get a nice index number
        idx = i * numaugs + j

        # generate the xml annotation file and save it
        tree = GenerateXML(str(idx) + ".jpg", str(idx) + ".jpg", width, height, label, x1s_out, y1s_out, x2s_out, y2s_out)  
        
     
        # image with BBs before/after augmentation (shown below)
        image_before = bbs.draw_on_image(image, thickness=2)
        image_after = bbs_aug.draw_on_image(image_aug, thickness=2, color=[0, 0, 255])

        imagetosave = image_aug

        if idx < numTestImages:
            SaveAnnotation(outputDir + "test/", tree, i * numaugs + j )
            cv2.imwrite(outputDir + "test/" + str(idx) + ".jpg", imagetosave)
        else:
            SaveAnnotation(outputDir + "train/", tree, i * numaugs + j )
            cv2.imwrite(outputDir + "train/" + str(idx) + ".jpg", imagetosave)

        SaveAnnotation(outputDir, tree, i * numaugs + j )
        cv2.imwrite(outputDir + str(idx) + ".jpg", imagetosave)
        cv2.imwrite(outputDir + "debug/" + str(idx) + ".jpg", image_before)
    

