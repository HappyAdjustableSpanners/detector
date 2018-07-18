import os
import glob
import pandas as pd
import xml.etree.ElementTree as ET
import sys
import argparse

parser = argparse.ArgumentParser(description='Description of your program')
parser.add_argument('-a','--annotationPath', help='Description for foo argument', required=True)
parser.add_argument('-o','--outputPath', help='Description for bar argument', required=True)
args = vars(parser.parse_args())

def xml_to_csv(path):
    xml_list = []
    for xml_file in glob.glob(path + '/*.xml'):
        tree = ET.parse(xml_file)
        root = tree.getroot()
        for member in root.findall('object'):
            value = (root.find('filename').text,
                     int(root.find('size')[0].text),
                     int(root.find('size')[1].text),
                     member[0].text,
                     int(member[4][0].text),
                     int(member[4][1].text),
                     int(member[4][2].text),
                     int(member[4][3].text)
                     )
            xml_list.append(value)
    column_name = ['filename', 'width', 'height', 'class', 'xmin', 'ymin', 'xmax', 'ymax']
    xml_df = pd.DataFrame(xml_list, columns=column_name)
    return xml_df

def main():

    for directory in ['train', 'test']:

        # ERROR
        print(args['annotationPath'])
        image_path = args['annotationPath'] + '/{}'.format(directory)
        print(image_path)
        xml_df = xml_to_csv(image_path)

        # output dir FINE
        xml_df.to_csv(os.path.join(args['outputPath'], '{}_labels.csv'.format(directory)), index=None)


main()