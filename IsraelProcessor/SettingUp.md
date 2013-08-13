# Setting Up

First we need to register the service at a given URL:

    netsh http add urlacl url=https://+:56341/israelprocessor user=michael.hunter

We need to make a trusted certificate to use as the certificate authority:

    "C:\Program Files (x86)\Microsoft SDKs\Windows\v7.1A\Bin\x64\makecert.exe" -sv SignRoot.pvk -cy authority -r signroot.cer -a sha1 -n "CN=Dev Certification Authority" -ss my -sr localmachine

This needs to be moved Personal -> Certificates to Trusted Root -> Certificates in mmc (run mmc, File->Add/Remove Snap-in->Certificates).

Then we need to create the certificate to use for the port:

    "C:\Program Files (x86)\Microsoft SDKs\Windows\v7.1A\Bin\x64\makecert.exe" -iv SignRoot.pvk -ic signroot.cer -cy end -pe -n CN="localhost" -eku 1.3.6.1.5.5.7.3.1 -ss my -sr localmachine -sky exchange -sp "Microsoft RSA SChannel Cryptographic Provider" -sy 12

Note that "localhost" needs to be whatever will be used to access the service.

Then we need to register the certificate with the port:

    netsh http add sslcert ipport=0.0.0.0:56341 certhash=b546a7907f7b5e3c78bf955b5186de308aa65163 appid={27de3452-f331-4486-b4bd-0d606b3009f2}

Note that the certificate hash is found details of the certificate in mmc as "Thumbprint".

Source: [http://www.codeproject.com/Articles/24027/SSL-with-Self-hosted-WCF-Service](http://www.codeproject.com/Articles/24027/SSL-with-Self-hosted-WCF-Service)