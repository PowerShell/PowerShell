Dev: XML legacy [ETS?]
What are some xml workloads we can simplify with XML parsing? 
Maybe cracking open some repository metadata? 
JEE servers use XML pretty extensively for configuration. Perhaps a script to adjust JEE server settings? Obviously, parsing XML in bash is fraught with danger...

PS /etc/tomcat> $tomcatserverfile="/usr/share/tomcat/conf/server.xml"
PS /etc/tomcat> [xml]$tomcatconfig=Get-Content $tomcatserverfile
PS /etc/tomcat> $tomcatconfig.Server


port                  : 8005
shutdown              : SHUTDOWN
#comment              : { Security listener. Documentation at /docs/config/listeners.html
                          <Listener className="org.apache.catalina.security.SecurityListener" />
                          , APR library loader. Documentation at /docs/apr.html , Initialize Jasper prior to webapps are loaded. Documentation at
                        /docs/jasper-howto.html ,  Prevent memory leaks due to use of particular java/javax APIs...}
Listener              : {Listener, Listener, Listener, Listener...}
GlobalNamingResources : GlobalNamingResources
Service               : Service




PS /etc/tomcat> $tomcatconfig.Server.port
8005
PS /etc/tomcat> $tomcatconfig.Server.Listener

className                                                   SSLEngine
---------                                                   ---------
org.apache.catalina.core.AprLifecycleListener               on
org.apache.catalina.core.JasperListener
org.apache.catalina.core.JreMemoryLeakPreventionListener
org.apache.catalina.mbeans.GlobalResourcesLifecycleListener
org.apache.catalina.core.ThreadLocalLeakPreventionListener


PS /etc/tomcat> $tomcatconfig.Server.Service

name     #comment
----     --------
Catalina {The connectors can use a shared executor, you can define one or more named thread pools, ...

