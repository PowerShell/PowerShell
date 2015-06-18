# image_ps
#
# VERSION               0.0.2

FROM ubuntu:14.04
MAINTAINER Peter Honeder <peterhon@microsoft.com>

RUN apt-get update
RUN apt-get install -y openssh-server wget libstdc++6 libunwind8
RUN mkdir /var/run/sshd
#RUN echo 'root:asldkfj23l4kj2lkajsdf' | chpasswd
RUN echo 'root:pass' | chpasswd
RUN adduser --shell /opt/app_base/runps.sh --disabled-password --gecos "" test1
RUN echo 'test1:pass' | chpasswd
RUN sed -i 's/PermitRootLogin without-password/PermitRootLogin yes/' /etc/ssh/sshd_config

RUN cd /root && wget https://github.com/anilgulecha/shellinabox/releases/download/2.14.2/shellinabox_2.14-1_amd64.deb
RUN dpkg -i /root/shellinabox_2.14-1_amd64.deb

# SSH login fix. Otherwise user is kicked off after login
RUN sed 's@session\s*required\s*pam_loginuid.so@session optional pam_loginuid.so@g' -i /etc/pam.d/sshd

ENV NOTVISIBLE "in users profile"
RUN echo "export VISIBLE=now" >> /etc/profile

COPY exec_env/app_base/ /opt/app_base/
RUN chmod -R ugo+rX /opt/app_base

EXPOSE 22
EXPOSE 4201
#CMD /usr/sbin/sshd && /usr/bin/shellinaboxd -s /:AUTH:HOME:/home/test1/app_base/runps.sh -p 4201 -t -v
CMD /usr/sbin/sshd && /usr/bin/shellinaboxd -s /:LOGIN -p 4201 -t -v

