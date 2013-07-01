
rem NOTE - Need to modify some of the xsd
rem e.g. --> <xs:element name="Namespaces" minOccurs="1" maxOccurs="1">
rem and type="xs:bool"
rem "C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\xsd.exe" InjectionSpecification.xml

"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\xsd.exe" InjectionSpecification.xsd /classes

rem Win8
rem "C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\xsd.exe" InjectionSpecification.xsd /classes

pause