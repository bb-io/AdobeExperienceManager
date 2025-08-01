# Blackbird.io AEM Cloud

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

AEM Cloud (Adobe Experience Manager) is a Content Management System (CMS) that allows users and companies to easily build websites, apps, and manage web pages and content. AEM is used by developers and marketers to organize and distribute content across digital channels. This App supports both the on-premise version of AEM as well as the AEM as a Cloud Service version.

## Before setting up

Before you connect to AEM Cloud, you need to have the following:

- AEM Cloud instance is running and accessible from the Blackbird platform.
- The Blackbird AEM plugin installed on your AEM instance. Its distribution and installation instructions can be found [here](https://github.com/bb-io/AEM) (prerequisites 1-8) and an AEM maintainer/developer should perform this installation. Note that if you use the on-premise verison you should use [these instructions](https://github.com/bb-io/AEM/blob/main-aem-on-prem/README.md).
- You have a technical account and created a private key for it so you can obtain a certificate to connect to AEM (after installing the plugin). Below in the `Steps to create technical account` section, you can find the steps to create a technical account and a private key and how to obtain a certificate.
- You know your base URL for AEM environment. The base URL is the URL of your AEM instance, e.g. `https://author-xxxx-xxxxx.adobeaemcloud.com`.

## Steps to create technical account and get a certificate

1. Open [Cloud Manager](https://experience.adobe.com/cloud-manager/landing.html).
2. Select needed program. 
![image auth step 2](docs/images/auth_step_2.png)
3. Open Developer Console for needed Author environment. 
![image auth step 3](docs/images/auth_step_3.png)
4. Switch to `Integrations` tab and `Create new technical account`. 
![image auth step 4](docs/images/auth_step_4.png)
5. Unfold created private key and `View` the data. 
![image auth step 5](docs/images/auth_step_5.png)
6. Use the `Download` button to obtain the raw data and store it in a file or another location from which it will be used for integration. 
![image auth step 6](docs/images/auth_step_6.png)

## Connecting

1. Navigate to apps and search for **AEM**
2. Click _Add Connection_
3. Name your connection for future reference e.g., 'My AEM'
4. Fill in the following fields:
   - **Base URL**: Your AEM base URL (e.g., `https://author-xxxx-xxxxx.adobeaemcloud.com`)
   - **Integration JSON certificate**: Integration certificate in JSON format. Can be found in the Developer Console. Example: [ "ok": true, "integration": [ "imsEndpoint": "ims-na1.adobelogin.com", ... ] "statusCode": 200]
5. Click _Connect_
6. Confirm that the connection has appeared and the status is _Connected_

![connection](docs/images/connection.png)

## Actions

- **Search content**: Search for content based on provided criteria.
- **Download content**: Download content as HTML. Requires a content ID. This action supports next optional inputs:
   - **Include reference content**: If set to true, the action will include reference content in the downloaded HTML, this referenced content can be other pages, content fragments, experience fragments, etc.
- **Upload content**: Upload content from HTML. Requires a HTML file and target path as input. This action supports the following inputs:
   - **File** (mandatory): The HTML file to upload.
   - **Target page path** (mandatory): The path where the content will be uploaded to.
   - **Source language** (optional): The language path segment in the source content URL. Required for reference content uploads. Example: If your content path is '/content/my-site/en/us/page', specify '/en/us' as the source language.
   - **Target language** (optional): The language path segment to replace the source language. Required for reference content uploads. Example: To convert from '/content/my-site/en/us/page' to '/content/my-site/fr/fr/page', specify '/fr/fr' as the target language.
   - **Ignore reference content errors** (optional): When set to true, errors that occur while updating reference content will be ignored.

## Events

- **On content created or updated**: Polling event that periodically checks for new or updated content. If the any content are found, the event is triggered.
- **On tag added** Periodically checks for new content with any of the specified tags. If there is any content found, the event is triggered.

> **Note on compatible content**: Blackbird supports all content types in the default page hierarchy: pages, content fragments, experience fragments, assets, etc. We are currently working on also supporting 'Guides' and 'DITA' content.

## Example 

Here's an example of how to set up a translation workflow with `AEM` and `DeepL` apps that will automatically translate content in AEM and send it to DeepL for translation.

![AEM example](docs/images/aem_example.png)

Here's a notes on how the example works:
- The `On content created or updated` event is triggered every hour and checks for new or updated content in AEM. If any content is found, the event is triggered.
- In the loop, the `Download content` action downloads the content from AEM in HTML format.
- The `Translate document` action of `DeepL` app sends the downloaded content to DeepL for translation. 
- The `Replace using Regex` action of `Blackbird Utilities` app replaces the original path to the target one in the translated content.

![Utilitiy example](docs/images/utility_example.png)

- The `Upload content` action uploads the translated content back to AEM.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
