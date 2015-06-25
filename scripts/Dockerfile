# image_ps
#
# VERSION               0.0.1

#FROM ubuntu:14.04
FROM phusion/baseimage:latest
MAINTAINER Peter Honeder <peterhon@microsoft.com>

CMD ["/sbin/my_init"]

RUN ifconfig
RUN cat /etc/resolv.conf
RUN ping -c 1 8.8.8.8
RUN apt-get update
RUN apt-get install -y openssh-server wget libstdc++6 libunwind8
#RUN mkdir /var/run/sshd
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

#RUN mkdir /etc/service/sshd
#ADD sshd-init.sh /etc/service/sshd/run
RUN mkdir /etc/service/shellinaboxd
ADD shellinaboxd-init.sh /etc/service/shellinaboxd/run

EXPOSE 22
EXPOSE 4201

# start sshd in daemonized mode, start shellinaboxd in interactive mode (must be started that way using docker's CMD)
#CMD /usr/sbin/sshd && /usr/bin/shellinaboxd -s /:LOGIN -p 4201 -t -v

